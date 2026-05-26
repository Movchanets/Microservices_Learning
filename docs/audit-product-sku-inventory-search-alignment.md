# Audit: Product / SKU / Inventory / Search Alignment

**Date:** 2026-05-25
**Scope:** DDD event-driven microservice alignment across Catalog, Inventory, Search, and shared contracts

---

## 1. Architecture Overview (Current State)

```
Catalog (owns Product + SKU)
  ├─ ProductCreatedDomainEvent  →  ProductCreatedEvent (integration)  →  Search (NO consumer!)
  ├─ SkuCreatedDomainEvent      →  SkuCreatedIntegrationEvent         →  Search (updates price)
  │                                                                 →  Inventory (creates item)
  ├─ SkuPriceChangedDomainEvent →  SkuPriceChangedEvent               →  Search (updates price)
  ├─ SkuDeletedDomainEvent      →  SkuDeletedEvent                    →  Search, Inventory
  └─ ProductUpdatedDomainEvent  →  ProductUpdatedEvent                →  Search (re-indexes)
```

---

## 2. Critical Findings

### F1 — DbUpdateConcurrencyException Blocks ALL SKU Operations (CRITICAL BUG — CONFIRMED)

**Severity:** Critical — **complete data loss**: Products exist but have ZERO SKUs. Frontend shows nothing.

**Evidence from seeder logs (Aspire run 2026-05-25):**
```
POST /api/catalog/products          → 201 Created ✅ (product created)
POST /api/catalog/products/{id}/skus → 409 Conflict ❌ EVERY TIME
PUT  /api/catalog/products/{id}/activate → 409 Conflict ❌ EVERY TIME
POST /api/cart/items                → 400 "SKU '00000000-...' not found" ❌
```

**Every single AddSku call returns 409.** The DB has 13 products (Draft status, no SKUs). The Skus table is empty. The frontend `GET /api/catalog/products` returns `totalCount: 0` because the list endpoint filters by `ProductStatus.Active`, and no product can be activated (activation requires at least one active SKU).

**Root cause chain:**
1. `DomainEventDispatcherInterceptor.SavingChangesAsync()` dispatches domain events **before** the actual DB save
2. Each domain event handler calls `IPublishEndpoint.Publish()` (e.g., `ProductCreatedDomainEventHandler`, `SkuCreatedDomainEventHandler`)
3. MassTransit's EF Outbox (`AddEntityFrameworkOutbox<CatalogDbContext>`) writes to `OutboxState` table within the **same DbContext transaction**
4. `OutboxState` has `RowVersion byte[]` concurrency token (`ValueGeneratedOnAddOrUpdate`)
5. The interceptor's while loop dispatches all events → multiple `Publish()` calls → `OutboxState.RowVersion` mutated multiple times
6. When `base.SaveChangesAsync()` runs, EF Core detects `OutboxState` was modified after initial tracking → `DbUpdateConcurrencyException`
7. `GlobalExceptionMiddleware` catches it → returns HTTP 409 `"Data Conflict"`

**The problem is NOT cross-request concurrency.** It's **intra-transaction** — the OutboxState entity is mutated multiple times within a single `SaveChanges` call by the event handlers, and EF Core's change tracker flags it as a concurrency violation.

**Fix options (ordered by recommendation):**
1. **Dispatch events AFTER save** — move to `SavedChangesAsync` (post-save hook). Events are dispatched in a separate DB transaction. Outbox writes don't conflict with the main entity save.
2. **Remove `RowVersion` from OutboxState** — override MassTransit's default config to exclude the concurrency token from the `OutboxState` entity mapping.
3. **Add retry loop for `DbUpdateConcurrencyException`** — wrap `SaveChangesAsync` in a retry policy (Polly or manual). Treats the symptom, not the cause.
4. **Use `UseBusOutbox()` at the bus level** — different concurrency model, but requires MassTransit config changes across all services.

---

### F2 — Search Document is Product-Level, Not SKU-Level (DESIGN MISMATCH)

**Severity:** High — search can't represent multi-SKU products correctly

**Current state:**
- `ProductSearchDocument` has a single `Price`, `Currency`, and `Sku` field
- `SkuCreatedConsumer` in Search just calls `UpdateProductPriceAsync()` — last-write-wins
- No `ProductCreatedEvent` consumer in Search — products aren't indexed until a SKU is added
- `SkuPriceChangedConsumer` updates the single price field

**Problem:** A product with 3 SKUs (e.g., T-shirt in S/M/L at different prices) will only show the price of the **last SKU created**. Search can't filter by SKU-specific attributes (size, color).

**Fix:** Search document should store a list of SKU objects:
```csharp
public sealed class ProductSearchDocument {
    // ... product fields ...
    public List<SkuSearchData> Skus { get; init; } = [];
    public decimal MinPrice => Skus.Min(s => s.Price);
    public decimal MaxPrice => Skus.Max(s => s.Price);
}

public sealed class SkuSearchData {
    public Guid SkuId { get; init; }
    public string SkuCode { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; }
    public Dictionary<string, string> Attributes { get; init; }
}
```

---

### F3 — ProductCreatedEvent Has No Price/SKU Data (CONTRACT GAP)

**Severity:** Medium — integration event is incomplete

**Current state:**
```csharp
public sealed record ProductCreatedEvent(
    Guid ProductId, string Name, string Description,
    Guid CategoryId, string CategoryName, List<string> Tags,
    string? ImageUrl, Guid StoreId, DateTime CreatedAt, string? Brand);
// No Price, no Currency, no SkuCode
```

**Problem:** Downstream consumers (Search, Inventory) can't create a complete product index from `ProductCreatedEvent` alone. They must wait for `SkuCreatedIntegrationEvent` to get price/SKU data.

**Impact:**
- Search doesn't have a `ProductCreatedEvent` consumer → products are invisible until a SKU is added
- The 2-step flow (ProductCreated → SkuCreated) creates a race condition where the product exists in Catalog but is unsearchable

**Fix:** Either:
1. Add `SkuCreatedDomainEvent` to the Product.Create() flow (create default SKU atomically)
2. Or add a `ProductCreatedEvent` consumer in Search that indexes the product with `Price = 0, Sku = ""` as placeholder

---

### F4 — Search Document Has Legacy `Sku` String Field (DEAD CODE)

**Severity:** Low — misleading model

**Current state:**
```csharp
public sealed class ProductSearchDocument {
    public string Sku { get; init; } = string.Empty;  // ← Legacy, should be Skus list
}
```

**Problem:** After the SKU refactor, a product has multiple SKUs. The single `Sku` string field is a leftover from when Product had one SKU. It's populated by `SkuCreatedConsumer` with the last SKU code.

---

### F5 — No `ProductCreatedEvent` Consumer in Search (MISSING HANDLER)

**Severity:** High — products are invisible in search until SKU is added

**Current state:** Search.API registers consumers for:
- `SkuCreatedConsumer` ✓
- `SkuDeletedConsumer` ✓
- `SkuPriceChangedConsumer` ✓
- `ProductDeletedConsumer` ✓
- `ProductCreatedConsumer` ✗ **MISSING**
- `ProductUpdatedConsumer` ✗ **MISSING**

**Impact:** When a seller creates a product (Draft status), it's not indexed in Elasticsearch at all. Only when a SKU is added does the product appear in search. If the seller creates a product but doesn't add a SKU immediately, the product is invisible to buyers.

---

### F6 — Inventory Uses `Version` for Concurrency, Catalog Does Not (INCONSISTENCY)

**Severity:** Low — inconsistency across services

**Current state:**
- `InventoryItem` has `byte[] Version` for optimistic concurrency
- `Product` has **no** concurrency token
- `Sku` has **no** concurrency token

**Problem:** Two concurrent requests to add different SKUs to the same product could theoretically conflict. The `Product` aggregate is modified (SKUs list changes) but has no concurrency protection.

**Fix:** Add a `RowVersion` property to `Product` aggregate and configure it as a concurrency token in `ProductConfiguration`.

---

## 3. Event Flow Gaps

| Event | Catalog Publishes | Search Consumes | Inventory Consumes | Gap |
|---|---|---|---|---|
| ProductCreated | ✓ | ✗ | ✗ (legacy) | Search missing consumer |
| ProductUpdated | ✓ | ✗ | — | Search missing consumer |
| ProductDeleted | ✓ | ✓ | — | OK |
| SkuCreated | ✓ | ✓ (price only) | ✓ | Search should index full SKU |
| SkuPriceChanged | ✓ | ✓ | — | OK |
| SkuDeleted | ✓ | ✓ | ✓ | OK |

---

## 4. Seeder Flow Issues

The seeder (`ProductSeeder.cs`) sends 3 sequential HTTP requests:
1. `POST /api/catalog/products` — creates product (fires `ProductCreatedDomainEvent`)
2. `POST /api/catalog/products/{id}/skus` — adds SKU (fires `SkuCreatedDomainEvent`)
3. `PUT /api/catalog/products/{id}/activate` — activates (fires `ProductUpdatedDomainEvent`)

Each request triggers domain events → MassTransit Outbox writes → potential `DbUpdateConcurrencyException` on `OutboxState.RowVersion`.

**The seeder should send `Price`, `Currency`, `Sku` in the `CreateProductCommand`** so the product + default SKU are created atomically in one `SaveChanges` call, avoiding the 3-step race.

---

### F7 — Cascade Failure: Empty Skus → Empty Cart → Failed Checkout (CONFIRMED)

**Severity:** Critical — end-to-end flow completely broken

**Evidence from seeder logs:**
```
13 products created (Draft status, no SKUs)
0 SKUs in database
Cart add → 400 "SKU '00000000-0000-0000-0000-000000000000' not found"
Cart has 0 items, total: $0.00
Checkout → 400 "Cart is empty."
```

**Cascade chain:**
```
DbUpdateConcurrencyException (F1)
  → Skus table empty
    → Products can't be activated (no active SKUs)
      → Product listing returns totalCount: 0 (filters by Active status)
        → Frontend shows nothing
    → Cart can't add items (SKU not found)
      → Cart is empty
        → Checkout fails ("Cart is empty.")
    → Inventory has items but no SKUs to link to
    → Search has no documents (no SkuCreated events fired)
```

**The `00000000-0000-0000-0000-000000000000` SKU ID** in the cart error indicates the OrderFlowSeeder is passing a default/empty Guid as the SKU reference — because the product DTOs returned by the API have empty SKU lists, and the seeder can't resolve a real SKU ID.

---

### F8 — Seeder Swallows SKU Errors, Reports False Success (DESIGN FLAW)

**Severity:** Medium — misleading status

**Evidence:**
```
warn: Failed to add SKU for Levi's 501 Original Jeans: Conflict - {...}
info: Created product: Levi's 501 Original Jeans   ← logged AFTER SKU failure!
```

The seeder logs "Created product" even when the SKU addition failed. The `ProductSeeder.EnsureProductExistsAsync()` method returns the product ID regardless of whether the SKU was added. The caller treats this as success.

**Fix:** The seeder should check SKU response status before reporting success, or better yet, the CreateProduct command should accept Price/Currency/Sku and create the default SKU atomically.

---

## 5. Recommended Fix Priority

| # | Finding | Priority | Effort |
|---|---|---|---|
| F1 | DbUpdateConcurrencyException blocks ALL SKU ops | **P0** | Medium |
| F7 | Cascade failure: empty Skus → empty cart → failed checkout | **P0** | (fixed by F1) |
| F5 | Missing ProductCreated consumer in Search | **P1** | Low |
| F2 | Search document is product-level, not SKU-level | **P1** | Medium |
| F3 | ProductCreatedEvent has no price/SKU data | **P2** | Low |
| F8 | Seeder swallows SKU errors, reports false success | **P2** | Low |
| F6 | No concurrency token on Product/Sku | **P2** | Low |
| F4 | Legacy Sku field in search document | **P3** | Low |

---

## 6. Files Referenced

| File | Role |
|---|---|
| `src/BuildingBlocks/Infrastructure/Database/Interceptors/DomainEventDispatcherInterceptor.cs` | Dispatches events before save |
| `src/BuildingBlocks/Infrastructure/Database/DomainEventsDbContext.cs` | Base DbContext with transaction management |
| `src/Microservices/Catalog/Catalog.Domain/Aggregates/Product.cs` | Product aggregate root |
| `src/Microservices/Catalog/Catalog.Infrastructure/EventPublishing/*.cs` | Domain event → integration event handlers |
| `src/Microservices/Catalog/Catalog.Infrastructure/Persistence/CatalogDbContext.cs` | MassTransit Outbox config |
| `src/Microservices/Search/Search.API/Models/Models.cs` | ProductSearchDocument (product-level) |
| `src/Microservices/Search/Search.API/Consumers/SkuCreatedConsumer.cs` | Updates single price field |
| `src/Microservices/Inventory/Inventory.Domain/Aggregates/InventoryItem.cs` | Has Version concurrency token |
| `src/Tools/Seeder.App/Seeders/ProductSeeder.cs` | 3-step create flow |
| `src/BuildingBlocks/SharedContracts/Events/Catalog/ProductCreatedEvent.cs` | Missing Price/SKU fields |
