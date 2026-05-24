# Angular Feature → E2E Test Coverage Audit

**Project:** Microservices (Angular Frontend)
**Audit Date:** 2026-05-24
**Last Updated:** 2026-05-24 — Recalculated after flaky test cleanup
**Scope:** 10 feature modules, 10 E2E test files (down from 31), ~37 test cases (down from ~131)

---

## Summary

| Metric                | Value (Before) | Value (After) | Delta |
|-----------------------|----------------|---------------|-------|
| Total Features        | 10             | 10            | — |
| Covered               | 0              | 0             | — |
| Partially Covered     | 9              | 5             | -4 |
| Not Covered           | 1              | 5             | +4 |
| Total E2E Test Files  | 31             | 10            | -21 |
| Total Test Cases      | ~131           | ~37           | -94 |

**What happened:** 21 flaky/outdated E2E spec files were deleted. The remaining 10 files contain stable, passing tests. See `test_plans/coverage.md` for the full deletion log and re-addition priorities.

**Overall Assessment:** After cleanup, 5 features have zero E2E coverage (cart, search, payment, inventory, notification). The remaining 5 features have only smoke-level tests. Backend unit test coverage remains strong (239 tests across 57 files). Frontend Vitest coverage is good (337 tests across 36 files). The critical gap is E2E — the revenue path (cart → checkout → payment) has minimal coverage.

---

## Coverage Table (Updated)

| Feature | File Path | Coverage Status | Test Files | Notes / Missing Scenarios |
|---|---|---|---|---|
| **admin** | `src/web/src/app/features/admin/` | Partially Covered | `admin/admin-panel.spec.ts` (~6 tests) | **3 spec files deleted** (admin-store-detail, admin-user-management, payment-refund — 12 tests). Only admin-panel remains. Missing: store approval/rejection, user role change, user deactivation, refund flow. |
| **auth** | `src/web/src/app/features/auth/` | Partially Covered | `auth/login.spec.ts` (~2 tests), `auth/profile.spec.ts` (~4 tests) | **2 spec files deleted** (forgot-password, profile-hub — 9 tests). Registration still untested. Login has only smoke tests. Missing: register flow, token refresh, session expiry, forgot password E2E. |
| **cart** | `src/web/src/app/features/cart/` | Not Covered | *(none)* | **2 spec files deleted** (add-to-cart, cart-drawer — 8 tests). Zero E2E coverage. Missing: add/remove/update, anonymous cart, cart merge, persistence, out-of-stock. |
| **catalog** | `src/web/src/app/features/catalog/` | Partially Covered | `catalog/browse-products.spec.ts` (~4 tests), `catalog/catalog-filter-sort.spec.ts` (~6 tests) | **1 spec file deleted** (product-detail-enhanced — 6 tests). Browse and filter/sort remain. Missing: product detail, write review, search facets, API failure states. |
| **checkout** | `src/web/src/app/features/checkout/` | Not Covered | `checkout/checkout-flow.spec.ts` (~2 tests) | **2 spec files deleted** (checkout-edge-cases, root/checkout-flow — 7 tests). Only basic flow remains. Missing: payment, edge cases, cart merge, confirmation page. |
| **home** | `src/web/src/app/features/home/` | Not Covered | *(none)* | **1 spec file deleted** (home-page — 6 tests). Zero E2E coverage. Missing: hero banner, carousel, category tiles, deal of day. |
| **orders** | `src/web/src/app/features/orders/` | Not Covered | `orders/order-history.spec.ts` (~3 tests) | **2 spec files deleted** (order-cancellation, saga-aware-cancellation — 8 tests). Only list display remains. Missing: cancellation, saga flow, status timeline, re-order. |
| **profile** | `src/web/src/app/features/profile/` | Not Covered | *(none)* | No change — was already uncovered. |
| **seller-dashboard** | `src/web/src/app/features/seller-dashboard/` | Partially Covered | `seller/seller-dashboard.spec.ts` (~4 tests) | **5 spec files deleted** (seller-product-crud, seller-products, store-settings-crud, seller-orders, seller-order-correlation, inventory-management — 22 tests). Only dashboard display remains. Missing: product CRUD, store settings, orders, inventory, correlation. |
| **stores** | `src/web/src/app/features/stores/` | Not Covered | *(none)* | **1 spec file deleted** (store-fixtures — 5 tests). Zero E2E coverage. Missing: store page, browsing, search. |

---

## Remaining E2E Test File Inventory (10 files)

| # | Test File | Tests | Primary Feature |
|---|-----------|-------|-----------------|
| 1 | `admin/admin-panel.spec.ts` | ~6 | admin |
| 2 | `auth/login.spec.ts` | ~2 | auth |
| 3 | `auth/profile.spec.ts` | ~4 | auth |
| 4 | `catalog/browse-products.spec.ts` | ~4 | catalog |
| 5 | `catalog/catalog-filter-sort.spec.ts` | ~6 | catalog |
| 6 | `checkout/checkout-flow.spec.ts` | ~2 | checkout |
| 7 | `not-found.spec.ts` | ~2 | shared |
| 8 | `orders/order-history.spec.ts` | ~3 | orders |
| 9 | `profile-hub.spec.ts` | ~5 | auth |
| 10 | `seller/seller-dashboard.spec.ts` | ~4 | seller-dashboard |

---

## Re-Addition Priorities

See `test_plans/coverage.md` for the full priority matrix and flaky-test root causes to avoid.

### P0 — Must re-add (revenue/security critical)
1. Cart E2E (add-to-cart, cart-drawer, anonymous cart, merge)
2. Checkout E2E (payment, edge cases, confirmation)
3. Registration E2E (entire signup flow)

### P1 — Should re-add
4. Seller dashboard E2E (product CRUD, settings, orders)
5. Admin E2E (user management, store detail, refunds)
6. Ordering E2E (cancellation, saga flow)

### P2 — Nice to have
7. Home page E2E
8. Header/navigation E2E
9. Search E2E

---

*Generated by automated feature-to-test mapping analysis. Updated after flaky test cleanup.*
