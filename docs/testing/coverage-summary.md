# Test Coverage Summary

**Project:** Marketplace Microservices
**Last Updated:** 2026-06-19

---

## Overall Test Totals

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Backend Unit (xUnit) | 57 | 239 | ✅ Good |
| Backend Integration | 13 | 51 | ⚠️ Needs work |
| Backend Contract | 11 | 51 | ✅ Good |
| Frontend Unit (Vitest) | 36 | 337 | ✅ Good |
| E2E (Playwright) | 17 | ~63 | ⚠️ Critical gap |
| **Total** | **134** | **~741** | |

---

## Per-Feature Coverage Matrix

| Feature | Unit | Integration | Contract | E2E | Overall |
|---------|------|-------------|----------|-----|---------|
| Identity | ✅ 40 | ⚠️ 10 | ⚠️ 5 | ⚠️ 6 | **Partial** |
| Catalog | ✅ 45 | ⚠️ 15 | ✅ 15 | ⚠️ 10 | **Partial** |
| Cart | ✅ 35 | ⚠️ 10 | ✅ 5 | ❌ 0 | **Gap** |
| Search | ✅ 12 | ⚠️ 8 | ✅ 5 | ❌ 0 | **Gap** |
| Ordering | ✅ 35 | ⚠️ 8 | ✅ 5 | ⚠️ 3 | **Partial** |
| Payment | ✅ 25 | ❌ 0 | ✅ 5 | ❌ 0 | **Gap** |
| Inventory | ✅ 18 | ⚠️ 10 | ✅ 5 | ❌ 0 | **Gap** |
| Notification | ✅ 15 | ❌ 0 | ⚠️ 5 | ❌ 0 | **Gap** |
| StoreManagement | ✅ 25 | ❌ 0 | ❌ 0 | ⚠️ 4 | **Gap** |
| Admin | ❌ 0 | ❌ 0 | ❌ 0 | ⚠️ 6 | **Gap** |
| Checkout | ❌ 0 | ❌ 0 | ❌ 0 | ⚠️ 2 | **Critical** |
| Home/Shared | ✅ 25 | — | — | ⚠️ 13 | **Partial** |
| BuildingBlocks | ✅ 4 | — | — | — | **OK** |
| ApiGateway | ✅ 3 | ⚠️ 2 | — | — | **Partial** |

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
| E2E Test Files | 31 | 17 | -14 |
| E2E Test Cases | ~131 | ~63 | -68 |
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

*This document summarizes test coverage across the Marketplace Microservices project.*