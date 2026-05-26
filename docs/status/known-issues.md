# Known Issues & Blockers

> **Last Updated:** 2026-05-26
> **Severity Legend:** 🔴 Critical · 🟠 High · 🟡 Medium · 🔵 Low

---

## Active Blockers

### 🔴 Seeder AddSku Returns 409 — No Products in Catalog

| Field | Value |
|:---|:---|
| **Severity** | 🔴 Critical (P0) |
| **Status** | ❌ Open |
| **Component** | Catalog.API — `AddSkuHandler` |
| **Impact** | Products created but never get SKUs → activation fails → 0 products in catalog → browse/cart/checkout E2E tests fail |
| **Root Cause** | Unhandled `InvalidOperationException` escapes `AddSkuHandler`. `GlobalExceptionMiddleware` maps it to 409. OTEL log dedup hides the actual error message. |
| **Likely Culprit** | `AddSkuValidator` regex `^[A-Za-z0-9][A-Za-z0-9\-]{1,48}[A-Za-z0-9]$` or category's `ValidateRequiredAttributes()` |
| **Fix** | Add try-catch logging in `AddSkuHandler` or disable OTEL log dedup to surface the actual error |

---

### 🟠 Seller Dashboard E2E Tests Fail (3 tests)

| Field | Value |
|:---|:---|
| **Severity** | 🟠 High (P1) |
| **Status** | ❌ Open |
| **Component** | Angular SPA — seller dashboard routes |
| **Impact** | Seller pages (dashboard, products, store settings) not rendering for E2E tests |
| **Root Cause** | Auth or routing issue — seller dashboard heading not visible, "Store Settings" heading not found |
| **Failed Tests** | `seller/seller-dashboard.spec.ts` × 3, `seller/product-sku-crud.spec.ts` × 1 |

---

## Event-Driven Integration Gaps

> Source: `DDD_EventAlignment_Audit.md`

### 🔴 SkuDeletedDomainEvent Has No Handler

| Field | Value |
|:---|:---|
| **Severity** | 🔴 Critical |
| **Status** | ❌ Open |
| **Location** | `Catalog.Infrastructure/EventPublishing/` — missing `SkuDeletedDomainEventHandler.cs` |
| **Impact** | Inventory never deactivates deleted SKU items → phantom stock reservable. Search shows deleted SKUs. Cart allows checkout against non-existent variants. |

### 🔴 Sku.ChangePrice() Fires No Domain Event

| Field | Value |
|:---|:---|
| **Severity** | 🔴 Critical |
| **Status** | ❌ Open |
| **Location** | `Catalog.Domain/Entities/Sku.cs:60-64` |
| **Impact** | Cart prices go stale. Search shows outdated prices. No audit trail. |
| **Missing** | `SkuPriceChangedDomainEvent`, `SkuPriceChangedDomainEventHandler`, consumers in Cart/Search |

### 🔴 Cart Unaware of SKUs

| Field | Value |
|:---|:---|
| **Severity** | 🔴 Critical |
| **Status** | ❌ Open |
| **Location** | `Cart.Domain/Entities/ProductPrice.cs`, `Cart.Domain/Aggregates/CartItem.cs` |
| **Impact** | Cannot distinguish variants. Wrong price for multi-SKU products. OrderItemContract has no SkuId. |

### 🟠 Search Index Is Product-Level Only

| Field | Value |
|:---|:---|
| **Severity** | 🟠 High |
| **Status** | ❌ Open |
| **Location** | `Search.API/Models/Models.cs` |
| **Impact** | Only one SKU's data appears in search. No per-SKU faceting. Price filtering shows single price. |

### 🟠 OrderItemContract Lacks SkuId

| Field | Value |
|:---|:---|
| **Severity** | 🟠 High |
| **Status** | ❌ Open |
| **Location** | `SharedContracts/Dtos/OrderItemContract.cs` |
| **Impact** | Order items can't trace to specific SKU variant. Inventory reservation ambiguous for multi-SKU products. |

### 🟠 Inventory Reserves by ProductId, Not SkuId

| Field | Value |
|:---|:---|
| **Severity** | 🟠 High |
| **Status** | ❌ Open |
| **Location** | `Inventory.Application/Commands/ReserveStockCommandHandler.cs` |
| **Impact** | Could reserve from wrong SKU's stock. `GetByProductIdAsync` returns first match. |

### 🟡 InventoryItem ProductId Unique Index Conflict

| Field | Value |
|:---|:---|
| **Severity** | 🟡 Medium |
| **Status** | ❌ Open |
| **Location** | `Inventory.Infrastructure/Data/Configurations/InventoryItemConfiguration.cs:38` |
| **Impact** | `HasIndex(x => x.ProductId).IsUnique()` prevents multiple SKUs under same ProductId. Second SKU creation fails with constraint violation. |

### 🟡 ProductCreatedEvent Carries Stale Compat Fields

| Field | Value |
|:---|:---|
| **Severity** | 🟡 Medium |
| **Status** | ❌ Open |
| **Location** | `SharedContracts/Events/Catalog/ProductCreatedEvent.cs` |
| **Impact** | `Price=0m`, `Sku=""` fields propagate to Search (indexes 0.0) and Cart (creates empty ProductPrice). |

### 🟡 No Inventory Event for Stock Changes

| Field | Value |
|:---|:---|
| **Severity** | 🟡 Medium |
| **Status** | ❌ Open |
| **Location** | `Inventory.Domain/Aggregates/InventoryItem.cs` |
| **Impact** | `AddStock()` doesn't fire event. Search's `InStock` field can't be updated after restocking. |

### 🟡 Cart ProductUpdatedConsumer Overwrites with Stale Data

| Field | Value |
|:---|:---|
| **Severity** | 🟡 Medium |
| **Status** | ❌ Open |
| **Location** | `Cart.Infrastructure/Messaging/Consumers/ProductUpdatedConsumer.cs` |
| **Impact** | Calls `UpsertAsync` with `evt.Price` (0m) and `evt.Sku` ("") for new products → price goes to 0 after any product update. |

### 🔵 Inventory ProductCreatedConsumer Creates Phantom Records

| Field | Value |
|:---|:---|
| **Severity** | 🔵 Low |
| **Status** | ❌ Open |
| **Location** | `Inventory.Infrastructure/Messaging/Consumers/ProductCreatedConsumer.cs` |
| **Impact** | Creates InventoryItem with `Guid.NewGuid()` as placeholder SkuId. Duplicate records with real SkuCreatedConsumer. |

---

## Non-Blocking Issues

| Issue | Severity | Status | Notes |
|:---|:---:|:---:|:---|
| Scalar container FailedToStart | 🔵 | ❌ Open | API docs only — non-blocking |
| MediatR license warning | 🔵 | ⚠️ | No impact in dev, required for production |
| Docker daemon not running | 🔵 | ⚠️ | Container restarts may fail |
| Duplicate AppHost instances | 🟡 | ⚠️ | `taskkill /PID 48264 /F` to clean up |
| Angular frontend intermittent exit | 🟡 | ⚠️ | Port conflict or dependency crash — needs investigation |

---

## Resolved Issues

| Issue | Resolution | Date |
|:---|:---|:---|
| Inventory API — stock endpoints 500 | Generate SkuId when empty | 2026-05-25 |
| Cart API — ProductCreatedConsumer duplicate key | Skip processing when `evt.Sku` is empty | 2026-05-25 |
| Angular build — Missing DecimalPipe | Added import | 2026-05-25 |
| NuGet Security Vulnerabilities (136 warnings) | Updated OpenTelemetry packages | 2026-05-25 |
| Angular Compiler Warnings (NG8102, NG8113, NG8107) | Fixed type issues and imports | 2026-05-25 |
| Cart/Inventory/Ordering crash on startup | Created EF migrations | 2026-05-25 |
