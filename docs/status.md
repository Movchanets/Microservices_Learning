# Status — 2026-05-26

## Done

### F1 Fix: DbUpdateConcurrencyException (P0) ✅
- **Root cause**: `ProductRepository.Update()` marks entire aggregate as Modified → new child Sku gets UPDATE instead of INSERT. Plus `OutboxState.RowVersion` concurrency token mutated multiple times in same transaction.
- **Fix applied**:
  - `AddSkuHandler.cs` — use `context.Add(sku)` instead of `productRepository.Update(product)`; publish integration event directly (bypassing domain event interceptor)
  - All 6 DbContexts — `IsConcurrencyToken(false)` on `OutboxState.RowVersion`
  - `DomainEventDispatcherInterceptor.cs` — kept pre-save (required by `UseBusOutbox()` in test fixtures)
- **Verified**: Products load in frontend (13 Active, each with 1 SKU, prices correct)

### Frontend Cart SKU Fix ✅
- **Problem**: Cart API requires `{ productId, skuId, skuCode, quantity }` but frontend sent `{ productId, quantity }`
- **Fix applied** (full-stack):
  - Backend: `ProductListDto` + `ProductReadRepository` — added `DefaultSkuId`/`DefaultSkuCode` fields
  - Frontend models: `CartItemDetails` got `skuId`/`skuCode`, `ProductListItem` got `defaultSkuId`/`defaultSkuCode`
  - Frontend services: `CartService.addItem(productId, skuId, skuCode, qty)`, `updateItem(skuId, qty)`, `removeItem(skuId)`
  - Frontend stores: `CartStore.addToCart(productId, skuId, skuCode, qty)`
  - Components: `BuyBoxComponent` gets `skuId` input; `ProductCardComponent` emits full `ProductListItem`
  - Cart UI: drawer + page use `item.skuId` for track/update/remove
  - Callers: product-list, home-page, store-page, frequently-bought-together all pass `defaultSkuId`/`defaultSkuCode`
- **Verified**: 344 frontend tests pass, cart API accepts new format, `defaultSkuId`/`defaultSkuCode` populated in product list API

### Audit Document ✅
- `docs/audit-product-sku-inventory-search-alignment.md` — 8 findings documented (F1-F8)

### Skill Update ✅
- `code-refactoring-refactor-clean` → `references/domain-event-outbox-concurrency.md` updated with compound root cause

---

## Remaining Findings (from audit)

| # | Finding | Priority | Status |
|---|---|---|---|
| F1 | DbUpdateConcurrencyException blocks ALL SKU ops | P0 | ✅ Fixed |
| F7 | Cascade failure: empty Skus → empty cart → failed checkout | P0 | ✅ Fixed |
| F5 | Missing ProductCreated consumer in Search | P1 | Open |
| F2 | Search document is product-level, not SKU-level | P1 | Open |
| F3 | ProductCreatedEvent has no price/SKU data | P2 | Open |
| F8 | Seeder swallows SKU errors, reports false success | P2 | Open |
| F6 | No concurrency token on Product/Sku | P2 | Open |
| F4 | Legacy Sku field in search document | P3 | Open |
