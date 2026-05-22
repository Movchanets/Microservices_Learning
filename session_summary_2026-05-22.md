# Session Summary — 2026-05-22

## Goal
Fix Seeder.Worker so it can create users, assign roles, create stores, categories, products, and set inventory quantities end-to-end.

---

## What Was Done

### 1. UserSeeder: Role Promotion Fix
**File:** `src/Tools/Seeder.App/Seeders/UserSeeder.cs:65`
- `new { NewRole = "Seller" }` → `new { Role = "Seller" }`
- `UpdateUserRoleCommand` expects `Role`, not `NewRole`. Sellers were never promoted → store creation failed with 403.

### 2. SKU Lookup Endpoint
**New files:**
- `src/Microservices/Catalog/Catalog.Application/Queries/GetProductBySkuQuery.cs`
- `src/Microservices/Catalog/Catalog.Application/Queries/GetProductBySkuHandler.cs`

**Modified:**
- `src/Microservices/Catalog/Catalog.Application/Interfaces/IProductReadRepository.cs` — added `GetBySkuAsync`
- `src/Microservices/Catalog/Catalog.Infrastructure/Repositories/ProductReadRepository.cs` — implemented `GetBySkuAsync` using `Sku.Create(sku)` + `p.Sku == skuVo` (EF Core can't translate `p.Sku.Value` in Where clause)
- `src/Microservices/Catalog/Catalog.API/Endpoints/ProductEndpoints.cs` — added `GET /api/catalog/products/sku/{sku}`

### 3. MassTransit Queue Collision Fix (CRITICAL)
**Problem:** Inventory, Cart, and Search all register `ProductCreatedConsumer`. Without service-specific endpoint name prefixes, MassTransit generates the same queue name for all three. RabbitMQ round-robins messages — 7 went to Inventory, 6 to Cart (7+6=13 products).

**Fix:** Added `KebabCaseEndpointNameFormatter("service", false)` to `ConfigureEndpoints`:
- `src/Microservices/Inventory/Inventory.API/Program.cs` — prefix `"inventory"`
- `src/Microservices/Cart/Cart.API/Program.cs` — prefix `"cart"`
- `src/Microservices/Search/Search.API/Program.cs` — prefix `"search"`

Each service now gets its own queue (e.g., `inventory-product-created-consumer`, `cart-product-created-consumer`). Messages fan out to all queues via the same exchange — proper pub/sub.

### 4. Store Verification Endpoint
**File:** `src/Microservices/StoreManagement/StoreManagement.API/Endpoints/StoreEndpoints.cs`
- Removed body parameter from `POST /api/stores/{id}/verify`
- Hardcoded `IsApproved = true` — verify is always approve
- Seeder reverted to `PostAsync(..., null)`

### 5. Product Activation Endpoint
**New files:**
- `src/Microservices/Catalog/Catalog.Application/Commands/ActivateProduct/ActivateProductCommand.cs`
- `src/Microservices/Catalog/Catalog.Application/Commands/ActivateProduct/ActivateProductHandler.cs`

**Modified:** `src/Microservices/Catalog/Catalog.API/Endpoints/ProductEndpoints.cs` — added `PUT /api/catalog/products/{id}/activate`

Products are created as `Draft` by default. `ListProducts` filters by `Active`. Seeder's existing activate call now works.

### 6. Inventory set-stock Endpoint
**File:** `src/Microservices/Inventory/Inventory.API/Endpoints/InventoryEndpoints.cs`
- Added `PUT /api/inventory/items/{sku}/stock` — idempotent upsert (creates item if not exists, sets quantity)
- Added `SetStockRequest(int Quantity, Guid StoreId, Guid ProductId)` record

### 7. Seeder Refactored to Use set-stock
**Files:**
- `src/Tools/Seeder.App/Seeders/ProductSeeder.cs` — returns `Guid?` (ProductId) now
- `src/Tools/Seeder.App/Seeders/InventorySeeder.cs` — replaced 30s polling loop with single `PUT` call
- `src/Tools/Seeder.App/Worker.cs` — collects `productIds` dict, passes StoreId+ProductId to inventory seeder

### 8. Cart MassTransit Retry Policy
**File:** `src/Microservices/Cart/Cart.API/Program.cs`
- Added `cfg.UseMessageRetry` with `Incremental(5, 100ms, 100ms)` for `DbUpdateException`
- `ProductCreatedConsumer` and `ProductUpdatedConsumer` race on the same product (activate triggers `ProductUpdatedEvent` right after create)
- MassTransit retry catches the first failed attempt, creates new scope/DbContext, retry finds the record and updates

### 9. Cart UpsertAsync Cleanup
**File:** `src/Microservices/Cart/Cart.Infrastructure/Repositories/ProductPriceRepository.cs`
- Clean upsert: `FirstOrDefaultAsync` → update if found, add if not
- `.Local.FirstOrDefault` detach was unnecessary — MassTransit RetryFilter runs before ScopeConsumerFactory, so retries always get a fresh DbContext

---

## Remaining Issue: Inventory Quantity = 0

The `PUT /api/inventory/items/{sku}/stock` endpoint was added to the code but returns 404 at runtime. **Aspire needs a full restart** to deploy the new endpoint. After restart, the seeder will call `set-stock` directly — no polling, no consumer dependency.

The consumer-based `ProductCreatedEvent` → `ProductCreatedConsumer` → `InventoryItem(qty=0)` flow still works for Cart/Search sync. The seeder just doesn't depend on it for setting stock anymore.

---

## Architecture Notes

### MassTransit Pub/Sub
- `IPublishEndpoint.Publish()` fans out to all bound queues
- Each service needs a unique queue prefix via `KebabCaseEndpointNameFormatter("service", false)`
- Without prefix, same-named consumers across services collide on one queue (competing consumers)

### MassTransit Retry
- `RetryFilter` runs BEFORE `ScopeConsumerFactory` — retries get a fresh DI scope + DbContext
- `UseMessageRetry` on bus config applies to all endpoint consumers
- `Handle<DbUpdateException>()` retries only on DB constraint violations
- First-chance exceptions in logs are expected — MassTransit self-heals on retry

### Catalog Outbox
- `UseBusOutbox()` stores events in DB, delivered by background service
- Domain events dispatched via `DomainEventDispatcherInterceptor` during `SaveChangesAsync`
- Events published atomically with the DB transaction

### Product Lifecycle
- `Product.Create()` → `ProductStatus.Draft`
- `Product.Activate()` → `ProductStatus.Active`
- `ListProducts` filters by `Active` — products invisible until activated
