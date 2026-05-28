# TODOs and Gaps — 2026-05-28

> **Snapshot Date:** 2026-05-28  
> **Sources:** Audit findings (F1-F8), integration test failures, contract test failures

---

## By Feature

### Search ❌ Critical Gaps

| # | Gap | Priority | Impact |
|:---:|:---|:---:|:---|
| 1 | No SkuCreatedIntegrationEvent consumer | P1 | Price not synced to search index |
| 2 | Search document is product-level, not SKU-level | P1 | Can't search/filter by SKU attributes |
| 3 | Contract test failing (SkuCreatedIntegrationEvent) | P1 | Verifies Search-Catalog contract |
| 4 | Integration test currency mismatch | P2 | Test data issue |

### Inventory ⚠️ Test Failures

| # | Gap | Priority | Impact |
|:---:|:---|:---:|:---|
| 1 | ReservationConsumer mock assertions failing (3 tests) | P1 | Can't verify reservation flow |
| 2 | ReserveStockCommandHandler uses ProductId, not SkuId | P2 | Wrong lookup key |

### Seller Dashboard ⚠️ Missing Feature

| # | Gap | Priority | Impact |
|:---:|:---|:---:|:---|
| 1 | No sales summary endpoint | P2 | Dashboard can't show sales stats |

### Catalog ⚠️ Event Gaps

| # | Gap | Priority | Impact |
|:---:|:---|:---:|:---|
| 1 | ProductCreatedEvent has no price/SKU data | P2 | Downstream consumers can't get price |
| 2 | No concurrency token on Product/Sku | P2 | Concurrent edits may overwrite |

### Cart ⚠️ Sync Gap

| # | Gap | Priority | Impact |
|:---:|:---|:---:|:---|
| 1 | No SkuCreatedIntegrationEvent consumer | P2 | Cart cache not updated on SKU creation |

### Media ⚠️ Test Coverage

| # | Gap | Priority | Impact |
|:---:|:---|:---:|:---|
| 1 | No unit tests | P3 | No regression safety |
| 2 | No integration tests | P3 | No regression safety |

---

## By Priority

### P1 — Critical (3 items)

1. **Search: Implement SkuCreatedIntegrationEvent consumer** — price sync to search index
2. **Search: Redesign document for SKU-level data** — enable SKU attribute search
3. **Inventory: Fix reservation test assertions** — mock capture pattern broken

### P2 — Important (5 items)

1. **Search: Fix integration test currency mismatch** — test data issue
2. **Inventory: Switch lookup to SkuId** — correct key for reservation
3. **Catalog: Add price/SKU data to ProductCreatedEvent** — downstream consumers
4. **Catalog: Add concurrency tokens to Product/Sku** — prevent overwrites
5. **Seller Dashboard: Add sales summary endpoint** — dashboard stats

### P3 — Nice to Have (3 items)

1. **Media: Add unit tests** — regression safety
2. **Media: Add integration tests** — regression safety
3. **SharedContracts: Create missing .csproj** — unit test project

---

## Resolved Since 2026-05-26

| Issue | Feature | Status |
|:---|:---|:---:|
| F1: DbUpdateConcurrencyException blocks SKU ops | Catalog | ✅ Fixed |
| F7: Cascade failure (empty Skus → empty cart) | Cart | ✅ Fixed |
| Frontend cart SKU integration | Cart | ✅ Fixed |
| Cart/Inventory/Ordering EF migrations | All | ✅ Applied |

---

## Test Coverage Gaps

| Service | Unit | Integration | Contract |
|:---|:---:|:---:|:---:|
| Media.API | ❌ Empty | ❌ Empty | — |
| Payment.IntegrationTests | — | ❌ Empty | — |
| StoreManagement.IntegrationTests | — | ❌ Empty | — |
| Notification.IntegrationTests | — | ❌ Empty | — |
| Media.IntegrationTests | — | ❌ Empty | — |
| BuildingBlocks.SharedContracts.UnitTests | ❌ Missing .csproj | — | — |

---

## Summary

| Priority | Count | Features Affected |
|:---|:---:|:---|
| P1 Critical | 3 | Search, Inventory |
| P2 Important | 5 | Search, Inventory, Catalog, Seller Dashboard |
| P3 Nice to Have | 3 | Media, SharedContracts |
| Resolved | 4 | Catalog, Cart |
