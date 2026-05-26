# Test Coverage Summary

**Project:** Marketplace Microservices
**Last Updated:** 2026-05-26
**Source of Truth:** [feature-coverage-audit.md](../../feature-coverage-audit.md) · [test_plans/coverage.md](../../test_plans/coverage.md)

---

## Overall Test Totals

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Backend Unit (xUnit) | 57 | 239 | ✅ Good |
| Backend Integration | 13 | 51 | ⚠️ Needs work |
| Backend Contract | 11 | 51 | ✅ Good |
| Frontend Unit (Vitest) | 36 | 337 | ✅ Good |
| E2E (Playwright) | 13 | ~55 | ⚠️ Critical gap |
| **Total** | **130** | **~733** | |

---

## Per-Feature Coverage Matrix

| Feature | Unit | Integration | Contract | E2E | Overall | Detail |
|---------|------|-------------|----------|-----|---------|--------|
| Identity | ✅ 40 | ⚠️ 10 | ⚠️ 5 | ⚠️ 6 | **Partial** | [identity.md](../../test_plans/identity.md) |
| Catalog | ✅ 45 | ⚠️ 15 | ✅ 15 | ⚠️ 10 | **Partial** | [catalog.md](../../test_plans/catalog.md) |
| Cart | ✅ 35 | ⚠️ 10 | ✅ 5 | ❌ 0 | **Gap** | [cart.md](../../test_plans/cart.md) |
| Search | ✅ 12 | ⚠️ 8 | ✅ 5 | ❌ 0 | **Gap** | [search.md](../../test_plans/search.md) |
| Ordering | ✅ 35 | ⚠️ 8 | ✅ 5 | ⚠️ 3 | **Partial** | [ordering.md](../../test_plans/ordering.md) |
| Payment | ✅ 25 | ❌ 0 | ✅ 5 | ❌ 0 | **Gap** | [payment.md](../../test_plans/payment.md) |
| Inventory | ✅ 18 | ⚠️ 10 | ✅ 5 | ❌ 0 | **Gap** | [inventory.md](../../test_plans/inventory.md) |
| Notification | ✅ 15 | ❌ 0 | ⚠️ 5 | ❌ 0 | **Gap** | [notification.md](../../test_plans/notification.md) |
| StoreManagement | ✅ 25 | ❌ 0 | ❌ 0 | ⚠️ 4 | **Gap** | [store-management.md](../../test_plans/store-management.md) |
| Admin | ❌ 0 | ❌ 0 | ❌ 0 | ⚠️ 6 | **Gap** | [admin.md](../../test_plans/admin.md) |
| Checkout | ❌ 0 | ❌ 0 | ❌ 0 | ⚠️ 2 | **Critical** | [checkout.md](../../test_plans/checkout.md) |
| Home/Shared | ✅ 25 | — | — | ⚠️ 13 | **Partial** | [home-and-shared.md](../../test_plans/home-and-shared.md) |
| BuildingBlocks | ✅ 4 | — | — | — | **OK** | Embedded in unit tests |
| ApiGateway | ✅ 3 | ⚠️ 2 | — | — | **Partial** | Middleware tests |

---

## Coverage Legend

| Symbol | Meaning | Threshold |
|--------|---------|-----------|
| ✅ | Covered | 80%+ of planned tests exist |
| ⚠️ | Partially Covered | 30–79% of planned tests exist |
| ❌ | Not Covered | <30% of planned tests exist |

---

## E2E Flaky Test Cleanup Summary

21 E2E spec files (~94 tests) were deleted as flaky or outdated. See [e2e-tests.md](e2e-tests.md) for the full deletion log.

| Metric | Before Cleanup | After Cleanup | Delta |
|--------|---------------|---------------|-------|
| E2E Test Files | 31 | 13 | -18 |
| E2E Test Cases | ~131 | ~55 | -76 |
| Features Partially Covered | 9 | 5 | -4 |
| Features Not Covered | 1 | 5 | +4 |

---

## Priority Actions

### P0 — Critical Gaps (re-add with stable patterns)

1. **Cart E2E** — add-to-cart, cart-drawer, anonymous cart, merge on login
2. **Checkout E2E** — payment processing, edge cases, cart merge
3. **Registration E2E** — entire signup flow untested

### P1 — Important Gaps

4. **Seller dashboard E2E** — product CRUD, store settings, orders, inventory
5. **Admin E2E** — user management, store detail, refunds
6. **Ordering E2E** — cancellation, saga-aware cancellation
7. **Payment integration tests** — no integration test project exists

### P2 — Nice to Have

8. **Home page E2E** — hero, carousel, category tiles (partially covered now)
9. **Header/navigation E2E** — mega menu, auth state (partially covered now)
10. **Search E2E** — search results, facets, pagination

---

*Generated from `feature-coverage-audit.md` and `test_plans/` data.*
