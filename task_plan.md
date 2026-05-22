# Seeder.Worker Gap Analysis & Fix Plan

## Current Seeder Flow (Worker.cs)
```
1. Login admin (seeded by Identity migration)
2. Register sellers via POST /api/identity/auth/register
3. Promote sellers via PUT /api/identity/users/{id}/role
4. Login sellers to get tokens
5. Create stores via POST /api/stores (needs seller token + SellerId)
6. Verify stores via POST /api/stores/{id}/verify (admin token)
7. Create categories via POST /api/catalog/categories (admin token)
8. Create products via POST /api/catalog/products (seller token + categoryId + storeId)
9. Poll inventory for consumer-created items, then add stock
```

---

## GAP 1 (CRITICAL): PromoteSellersAsync sends wrong JSON field

**File:** `src/ToolsSeeder.App/Seeders/UserSeeder.cs:65`
**Code:**
```csharp
var roleResponse = await _client.PutAsJsonAsync(
    $"/api/identity/users/{existingUser.Id}/role",
    new { NewRole = "Seller" }, ct);
```
**Problem:** Sends `{ "newRole": "Seller" }` (camelCase via System.Net.Http.Json).
But `UpdateUserRoleCommand` has property `Role`, not `NewRole`.
Minimal API model binding fails → 400 Bad Request → sellers stay as Buyers.
→ `POST /api/stores` fails with 403 (requires "Seller" policy).
→ No stores → products skip (storeId == null).

**Fix:** Change `new { NewRole = "Seller" }` → `new { Role = "Seller" }`

---

## GAP 2 (BLOCKING): No GET /api/catalog/products/sku/{sku} endpoint

**File:** `src/Tools/Seeder.App/Seeders/ProductSeeder.cs:24`
**Code:**
```csharp
var getResponse = await _client.GetAsync($"/api/catalog/products/sku/{product.Sku}", ct);
```
**Problem:** Catalog ProductEndpoints has no SKU-based lookup route.
Call always returns 404 → seeder always tries to create (which is actually OK for first run).
But idempotency check fails on re-run → duplicate SKU errors.

**Fix:** Add `GetProductBySkuQuery` + endpoint in Catalog.API.

---

## GAP 3 (DESIGN): ProductItem creation relies on eventual consistency

**Current flow:**
- Catalog publishes `ProductCreatedEvent` on product creation
- Inventory `ProductCreatedConsumer` creates `InventoryItem` (sku, qty=0)
- Cart `ProductCreatedConsumer` creates `ProductPrice` entry
- Seeder polls `GET /api/inventory/items/{sku}` with 2s retry, 15 max retries
- Then calls `POST /api/inventory/items/{sku}/add-stock`

**Status:** This actually works correctly! The consumers handle creation.
The seeder's polling approach (wait for consumer) is a valid pattern.
No changes needed here — just needs GAPs 1 & 2 fixed so products get created.

---

## GAP 4 (MINOR): Store listing with seller token

**File:** `src/Tools/Seeder.App/Seeders/StoreSeeder.cs:27`
**Code:**
```csharp
var getStoresResponse = await _client.GetAsync("/api/stores", ct);
```
**Problem:** `GET /api/stores` is public, returns `ListStoresQuery(status)`.
The seller token is set in the header but not required for this endpoint.
This works fine — no fix needed.

---

## GAP 5 (MINOR): CreateStoreCommand SellerId type

**File:** `src/Tools/Seeder.App/Seeders/StoreSeeder.cs:52`
**Code:**
```csharp
var requestBody = new { SellerId = sellerId.ToString(), store.Name, store.Description };
```
**Status:** `CreateStoreCommand.SellerId` is `string`. The seeder sends Guid as string.
This is correct — no fix needed.

---

## Implementation Plan

### Phase 1: Fix PromoteSellersAsync (CRITICAL)
- [ ] 1.1 Fix `UserSeeder.cs:65`: `new { NewRole = "Seller" }` → `new { Role = "Seller" }`

### Phase 2: Add SKU lookup endpoint (BLOCKING for idempotency)
- [ ] 2.1 Create `Catalog.Application/Queries/GetProductBySku/GetProductBySkuQuery.cs`
- [ ] 2.2 Create `Catalog.Application/Queries/GetProductBySku/GetProductBySkuHandler.cs`
- [ ] 2.3 Add route `GET /api/catalog/products/sku/{sku}` to `ProductEndpoints.cs`

### Phase 3: Verify end-to-end (no code changes)
- [ ] 3.1 Run Aspire, verify seeder completes all 6 steps
- [ ] 3.2 Verify Inventory items created by consumer (polling works)
- [ ] 3.3 Verify Cart ProductPrice entries created by consumer

---

## Consumers & Events Audit

### Integration Events (SharedContracts)
| Event | Publisher | Consumers |
|-------|-----------|-----------|
| ProductCreatedEvent | Catalog.API | Inventory, Cart, Search |
| ProductUpdatedEvent | Catalog.API | Inventory, Cart, Search |
| ProductDeletedEvent | Catalog.API | Cart, Search |
| ProductPriceChangedEvent | Catalog.API | Cart |
| StoreVerifiedEvent | StoreManagement | Identity |
| OrderSubmittedEvent | Cart | Ordering |
| OrderStatusChangedEvent | Ordering | Notification |
| InventoryReservedEvent | Inventory | Ordering |
| PaymentCompletedEvent | Payment | Ordering |

### Consumers per service
| Service | Consumers |
|---------|-----------|
| Inventory | ProductCreatedConsumer, ProductUpdatedConsumer, ReserveInventoryConsumer, CancelReservationConsumer |
| Cart | ProductCreatedConsumer, ProductUpdatedConsumer, ProductDeletedConsumer, ProductPriceChangedConsumer |
| Search | ProductCreatedConsumer, ProductUpdatedConsumer, ProductDeletedConsumer |
| Ordering | OrderSubmittedConsumer, OrderInventoryReservedConsumer, OrderPaymentProcessingConsumer, OrderCancelledProjectionConsumer, OrderCompletedProjectionConsumer |
| Identity | StoreVerifiedConsumer |
| Notification | OrderStatusChangedConsumer, OrderCancelledConsumer, OrderCompletedConsumer |
| Payment | ProcessPaymentConsumer, RefundPaymentConsumer |

### Seeder → Consumer chain
```
POST /api/catalog/products
  → ProductCreatedEvent published
    → Inventory.ProductCreatedConsumer creates InventoryItem (qty=0)
    → Cart.ProductCreatedConsumer creates ProductPrice
    → Search.ProductCreatedConsumer indexes product
  → Seeder polls Inventory until item exists
  → POST /api/inventory/items/{sku}/add-stock
```

---

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| LINQ: `p.Sku.Value == sku` not translatable | 1 | Use `Sku.Create(sku)` + `p.Sku == skuVo` (matches domain repo pattern) |
| BadHttpRequest: verify endpoint expects body, seeder sends null | 1 | Changed `PostAsync(..., null)` to `PostAsJsonAsync(..., new { })` |
| MassTransit queue collision: Inventory + Cart + Search all had `ProductCreatedConsumer` with same queue name | 1 | Added `KebabCaseEndpointNameFormatter("service", false)` to `ConfigureEndpoints` in all 3 services |
