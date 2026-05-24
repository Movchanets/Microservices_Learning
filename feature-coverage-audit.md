# Angular Feature → E2E Test Coverage Audit

**Project:** Microservices (Angular Frontend)
**Audit Date:** 2026-05-24
**Scope:** 10 feature modules, 31 E2E test files, ~131 test cases
**Auditor:** Hermes Agent (Automated)

---

## Summary

| Metric                | Value |
|-----------------------|-------|
| Total Features        | 10    |
| Covered               | 0     |
| Partially Covered     | 9     |
| Not Covered           | 1     |
| Total E2E Test Files  | 31    |
| Total Test Cases      | ~131  |

**Overall Assessment:** No feature meets the bar for "Covered" status. Nine out of ten features have test presence but lack critical edge-case depth (error states, empty states, auth guard edge cases, API failure handling). One feature (`profile`) has no tests at all. The seller-dashboard module has the strongest test breadth (26 tests across 7 files) but still falls short on error/edge-case coverage. Authentication and cart flows — the two most security- and revenue-critical paths — have the weakest coverage relative to their complexity.

---

## Coverage Table

| Feature | File Path | Coverage Status | Test Files | Notes / Missing Scenarios |
|---|---|---|---|---|
| **admin** | `src/web/src/app/features/admin/` | Partially Covered | `admin/admin-panel.spec.ts` (6 tests), `admin/admin-store-detail.spec.ts` (2 tests), `admin/admin-user-management.spec.ts` (5 tests), `root/payment-refund.spec.ts` (5 tests) | **18 tests total.** Auth guards tested (redirect unauth, non-admin redirect). User management covers role change and deactivation. Refund flow covered. **Missing:** Error states for failed API calls (user role change failure, store approval failure). `stats-card` component has no dedicated tests. `store-verification` component only has approve test — missing rejection flow, pending queue, bulk actions. Admin search/filter for users not tested. |
| **auth** | `src/web/src/app/features/auth/` | Partially Covered | `auth/login.spec.ts` (2 tests), `auth/forgot-password.spec.ts` (4 tests), `auth/profile.spec.ts` (4 tests), `root/header.spec.ts` (3 tests), `root/profile-hub.spec.ts` (5 tests), `profile/profile-settings.spec.ts` (5 tests) | **23 tests across 6 files.** Profile display, logout, forgot-password (including error for non-existent user), and profile settings (name update, password change) are tested. **Critical gaps:** `register/register.ts` has ZERO tests — entire registration flow untested. Login has only 2 tests — missing: email format validation, password field empty, account locked/disabled states, "remember me", OAuth/SSO if applicable. No token refresh or expired session handling tests. No CSRF bypass verification (context: Gateway CSRF needs bypass). No test for profile settings saving with server error. |
| **cart** | `src/web/src/app/features/cart/` | Partially Covered | `cart/add-to-cart.spec.ts` (3 tests), `root/cart-drawer.spec.ts` (5 tests) | **8 tests across 2 files.** Add-to-cart from catalog and detail page tested. Mini-cart drawer open/close/empty/add tested. **Critical gaps:** No remove-item test. No update-quantity test. No anonymous cart test (BuyerId is nullable Guid?, uses `X-Cart-Id` header — entirely untested). No cart-merge-on-login test (checkout requires auth and must pass CartId for merge). No price recalculation on quantity change. No out-of-stock error handling. No cart persistence across page refresh. `cart.service.ts` and `cart.store.ts` edge cases (API errors, network failure) not covered. |
| **catalog** | `src/web/src/app/features/catalog/` | Partially Covered | `catalog/browse-products.spec.ts` (4 tests), `catalog/catalog-filter-sort.spec.ts` (6 tests), `root/product-detail-enhanced.spec.ts` (6 tests) | **16 tests across 3 files.** Browse, filter, sort, search, pagination, empty state all tested. Product detail covers buy box, stock indicator, quantity controls, reviews, frequently-bought-together. **Missing:** `write-review` component has no tests (review submission, validation, character limits). `search-facets` component interaction not tested. `review-list` pagination/sorting not tested. No test for API failure on product load. Catalog API quirk: `UpdateProductCommand` does NOT accept price (separate PATCH endpoint) — no test verifies this constraint. `review.service.ts` / `review.store.ts` error states untested. |
| **checkout** | `src/web/src/app/features/checkout/` | Partially Covered | `checkout/checkout-edge-cases.spec.ts` (5 tests), `checkout/checkout-flow.spec.ts` (2 tests), `root/checkout-flow.spec.ts` (2 tests) | **9 tests across 3 files.** Auth guard (redirect unauth), empty cart guard, required field validation, address form, and full checkout via API helpers tested. **Critical gaps:** No payment processing test (only "fill and proceed"). No checkout-status page test (order confirmation / failure). No test for cart merge behavior during checkout (CartId pass-through with authenticated user). No test for `checkout-summary` component accuracy. Context: MassTransit `UseBusOutbox()` is BROKEN — no saga-aware checkout completion test. No test for payment failure or retry. No test for checkout with expired cart. |
| **home** | `src/web/src/app/features/home/` | Partially Covered | `home/home-page.spec.ts` (6 tests) | **6 tests in 1 file.** All main components covered: hero banner, featured carousel, catalog navigation, add-to-cart from carousel, deal of the day. **Missing:** No error state test (API failure loading products/deals). No loading/skeleton state test. `category-tiles` component has no interaction test (click tile → navigate to category). No test for empty state when no deals available. No test for carousel edge cases (single item, many items, responsive behavior). |
| **orders** | `src/web/src/app/features/orders/` | Partially Covered | `orders/order-history.spec.ts` (3 tests), `root/order-cancellation.spec.ts` (5 tests), `root/saga-aware-cancellation.spec.ts` (3 tests) | **11 tests across 3 files.** Order list display, empty state, order detail navigation, cancel button, status badge, timeline, items list, saga-aware cancellation (completed=no cancel, cancel via API, cancelled status). **Missing:** No order filtering/sorting test. No order re-order/re-purchase test. No test for `order-timeline` event variations beyond cancellation. No test for `status-badge` color/icon variations across all statuses. No error state for failed cancellation API call. No test for order detail with multiple sellers. |
| **profile** | `src/web/src/app/features/profile/` | Not Covered | *(none)* | **0 tests.** This module contains only `components/saved-searches/saved-searches.ts`. No E2E test exists for saved searches (create, view, delete, navigate from saved search). Primary profile functionality lives under `auth/profile/` and is Partially Covered there. This feature is low-impact but completely untested. |
| **seller-dashboard** | `src/web/src/app/features/seller-dashboard/` | Partially Covered | `seller/seller-dashboard.spec.ts` (4 tests), `seller/seller-product-crud.spec.ts` (5 tests), `seller/seller-products.spec.ts` (3 tests), `seller/store-settings-crud.spec.ts` (4 tests), `root/inventory-management.spec.ts` (4 tests), `root/seller-orders.spec.ts` (4 tests), `root/seller-order-correlation.spec.ts` (2 tests) | **26 tests across 7 files.** Strongest coverage in the project. Auth guards, product CRUD with validation, inventory table with status filter, store settings display/update, seller orders with status updates, buyer-seller correlation all tested. **Missing:** `sales-card` component has no dedicated tests (metrics display, date range). Product price changes via separate PATCH endpoint not tested (catalog API constraint). No bulk inventory operation tests. No product image upload test. No store-settings validation error states. No inventory stock threshold / low-stock alert tests. No error handling for failed product creation. Closest to "Covered" status but lacks error-state depth. |
| **stores** | `src/web/src/app/features/stores/` | Partially Covered | `root/store-fixtures.spec.ts` (5 tests) | **5 tests in 1 file.** Tests are fixture/setup-oriented: create store, create product, idempotent creation, seller API, admin API. **Missing:** No test for `store-page` component display (store detail view for buyers). No store browsing/discovery test. No store search test. No store rating/review display test. Tests are infrastructure fixtures, not user-facing feature tests. |

---

## Top 3 High-Priority Features for Test Coverage

### 1. 🔴 auth (`src/web/src/app/features/auth/`)

**Why critical:** Authentication is the security perimeter for the entire application. It gates admin, checkout, seller-dashboard, and orders features.

**Gaps:**
- Registration flow (`register/register.ts`) is **completely untested** — no signup, email validation, password strength, duplicate account handling
- Login has only 2 smoke tests — no form validation, no locked/disabled account, no rate limiting awareness
- No token refresh or expired session redirect test
- No CSRF bypass verification despite the Gateway requiring it
- No test for auth interceptors handling 401 responses mid-session

**Recommended new tests:** ~8-10 (register happy path, register validation, register duplicate, login validation, token refresh, session expiry redirect, CSRF bypass, auth interceptor 401)

---

### 2. 🔴 cart (`src/web/src/app/features/cart/`)

**Why critical:** Cart is the revenue-conversion funnel. Untested anonymous cart and cart-merge flows directly risk lost sales.

**Gaps:**
- Anonymous cart (`X-Cart-Id` header, `BuyerId` nullable) is **entirely untested**
- Cart merge on authentication (checkout requires CartId pass-through) is **untested**
- No remove-item or update-quantity test — only add is covered
- No out-of-stock or inventory-change-after-add test
- No cart expiration/persistence test

**Recommended new tests:** ~8-10 (remove item, update quantity, anonymous cart create, cart merge on login, out-of-stock error, cart persistence, cart empty after order, API failure handling)

---

### 3. 🔴 checkout (`src/web/src/app/features/checkout/`)

**Why critical:** Checkout is the final conversion step. Context notes that MassTransit `UseBusOutbox()` is BROKEN and saga uses separate command from projection event — this integration is completely untested.

**Gaps:**
- No payment processing or payment failure test
- No order confirmation (`checkout-status`) page test
- No test for the broken outbox / saga completion path
- No test for cart merge during authenticated checkout
- No test for checkout with stale/expired cart
- No test verifying `checkout-summary` price accuracy

**Recommended new tests:** ~6-8 (payment success, payment failure, order confirmation page, saga-aware completion, cart merge checkout, expired cart handling, summary price accuracy)

---

## Appendix: Test File Inventory (31 files)

| # | Test File | Tests | Primary Feature |
|---|---|---|---|
| 1 | `admin/admin-panel.spec.ts` | 6 | admin |
| 2 | `admin/admin-store-detail.spec.ts` | 2 | admin |
| 3 | `admin/admin-user-management.spec.ts` | 5 | admin |
| 4 | `auth/forgot-password.spec.ts` | 4 | auth |
| 5 | `auth/login.spec.ts` | 2 | auth |
| 6 | `auth/profile.spec.ts` | 4 | auth |
| 7 | `cart/add-to-cart.spec.ts` | 3 | cart |
| 8 | `catalog/browse-products.spec.ts` | 4 | catalog |
| 9 | `catalog/catalog-filter-sort.spec.ts` | 6 | catalog |
| 10 | `checkout/checkout-edge-cases.spec.ts` | 5 | checkout |
| 11 | `checkout/checkout-flow.spec.ts` | 2 | checkout |
| 12 | `home/home-page.spec.ts` | 6 | home |
| 13 | `orders/order-history.spec.ts` | 3 | orders |
| 14 | `profile/profile-settings.spec.ts` | 5 | auth (profile) |
| 15 | `seller/seller-dashboard.spec.ts` | 4 | seller-dashboard |
| 16 | `seller/seller-product-crud.spec.ts` | 5 | seller-dashboard |
| 17 | `seller/seller-products.spec.ts` | 3 | seller-dashboard |
| 18 | `seller/store-settings-crud.spec.ts` | 4 | seller-dashboard |
| 19 | `root/cart-drawer.spec.ts` | 5 | cart |
| 20 | `root/checkout-flow.spec.ts` | 2 | checkout |
| 21 | `root/header-mega-menu.spec.ts` | 6 | shared (header) |
| 22 | `root/header.spec.ts` | 3 | shared (header) |
| 23 | `root/inventory-management.spec.ts` | 4 | seller-dashboard |
| 24 | `root/not-found.spec.ts` | 2 | shared (routing) |
| 25 | `root/order-cancellation.spec.ts` | 5 | orders |
| 26 | `root/payment-refund.spec.ts` | 5 | admin |
| 27 | `root/product-detail-enhanced.spec.ts` | 6 | catalog |
| 28 | `root/profile-hub.spec.ts` | 5 | auth (profile) |
| 29 | `root/saga-aware-cancellation.spec.ts` | 3 | orders |
| 30 | `root/seller-order-correlation.spec.ts` | 2 | seller-dashboard |
| 31 | `root/seller-orders.spec.ts` | 4 | seller-dashboard |
| 32 | `root/store-fixtures.spec.ts` | 5 | stores |

---

*Generated by automated feature-to-test mapping analysis.*
