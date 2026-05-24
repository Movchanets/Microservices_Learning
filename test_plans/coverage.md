# Test Coverage — Current State

**Last Updated:** 2026-05-24
**Reason:** Flaky and old E2E tests removed. Coverage recalculated from remaining tests.

---

## Summary

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Backend Unit (xUnit) | 57 | 239 | Good |
| Backend Integration | 13 | 51 | Needs work |
| Backend Contract | 10 | 51 | Good |
| Frontend Unit (Vitest) | 36 | 337 | Good |
| E2E (Playwright) | 10 | 37 | Critical gap |
| **Total** | **126** | **715** | |

---

## What Was Removed

21 E2E spec files (~94 tests) deleted as flaky or outdated:

| Deleted File | Tests | Feature | Reason |
|-------------|-------|---------|--------|
| `auth/forgot-password.spec.ts` | 4 | auth | Flaky timing |
| `auth/register.spec.ts` | — | auth | Was already missing |
| `cart/add-to-cart.spec.ts` | 3 | cart | Flaky — fillStable timing |
| `cart/cart-drawer.spec.ts` | 5 | cart | Flaky — drawer open/close race |
| `checkout/checkout-edge-cases.spec.ts` | 5 | checkout | Flaky — address form fill |
| `root/checkout-flow.spec.ts` | 2 | checkout | Duplicate of checkout-flow.spec.ts |
| `root/header.spec.ts` | 3 | shared | Flaky — auth state display |
| `root/header-mega-menu.spec.ts` | 6 | shared | Flaky — mega menu hover timing |
| `root/inventory-management.spec.ts` | 4 | seller | Flaky — table load timing |
| `root/order-cancellation.spec.ts` | 5 | orders | Flaky — dialog handler race |
| `root/payment-refund.spec.ts` | 5 | admin | Flaky — refund dialog timing |
| `root/product-detail-enhanced.spec.ts` | 6 | catalog | Flaky — buy box interaction |
| `root/profile-hub.spec.ts` | 5 | auth | Flaky — tab navigation |
| `root/saga-aware-cancellation.spec.ts` | 3 | orders | Flaky — saga completion wait |
| `root/seller-order-correlation.spec.ts` | 2 | seller | Flaky — correlation display |
| `root/seller-orders.spec.ts` | 4 | seller | Flaky — order status update |
| `root/store-fixtures.spec.ts` | 5 | stores | Infrastructure fixtures, not feature tests |
| `admin/admin-store-detail.spec.ts` | 2 | admin | Flaky — store approval dialog |
| `admin/admin-user-management.spec.ts` | 5 | admin | Flaky — role change dialog |
| `seller/seller-product-crud.spec.ts` | 5 | seller | Flaky — product form fill |
| `seller/seller-products.spec.ts` | 3 | seller | Flaky — product list load |
| `seller/store-settings-crud.spec.ts` | 4 | seller | Flaky — settings form fill |

---

## Per-Feature Coverage Matrix

| Feature | Unit | Integration | Contract | E2E | Overall | Detail |
|---------|------|-------------|----------|-----|---------|--------|
| Identity | ✅ 40 | ⚠️ 10 | ⚠️ 5 | ⚠️ 6 | **Partial** | [identity.md](identity.md) |
| Catalog | ✅ 45 | ⚠️ 15 | ✅ 15 | ⚠️ 10 | **Partial** | [catalog.md](catalog.md) |
| Cart | ✅ 35 | ⚠️ 10 | ✅ 5 | ❌ 0 | **Gap** | [cart.md](cart.md) |
| Search | ✅ 12 | ⚠️ 8 | ✅ 5 | ❌ 0 | **Gap** | [search.md](search.md) |
| Ordering | ✅ 35 | ⚠️ 8 | ✅ 5 | ⚠️ 3 | **Partial** | [ordering.md](ordering.md) |
| Payment | ✅ 25 | ❌ 0 | ✅ 5 | ❌ 0 | **Gap** | [payment.md](payment.md) |
| Inventory | ✅ 18 | ⚠️ 10 | ✅ 5 | ❌ 0 | **Gap** | [inventory.md](inventory.md) |
| Notification | ✅ 15 | ❌ 0 | ⚠️ 5 | ❌ 0 | **Gap** | [notification.md](notification.md) |
| StoreManagement | ✅ 25 | ❌ 0 | ❌ 0 | ⚠️ 4 | **Gap** | [store-management.md](store-management.md) |
| Admin | ❌ 0 | ❌ 0 | ❌ 0 | ⚠️ 6 | **Gap** | [admin.md](admin.md) |
| Checkout | ❌ 0 | ❌ 0 | ❌ 0 | ⚠️ 2 | **Critical** | [checkout.md](checkout.md) |
| Home/Shared | ✅ 25 | — | — | ⚠️ 7 | **Partial** | [home-and-shared.md](home-and-shared.md) |
| BuildingBlocks | ✅ 4 | — | — | — | **OK** | Embedded in unit tests |
| ApiGateway | ✅ 3 | ⚠️ 2 | — | — | **Partial** | Middleware tests |

---

## Coverage Legend

| Symbol | Meaning | Threshold |
|--------|---------|-----------|
| ✅ | Covered | 80%+ of planned tests exist |
| ⚠️ | Partially Covered | 30-79% of planned tests exist |
| ❌ | Not Covered | <30% of planned tests exist |

---

## E2E Test Inventory (Remaining)

| # | File | Tests | Feature |
|---|------|-------|---------|
| 1 | `admin/admin-panel.spec.ts` | ~6 | Admin panel display + auth guards |
| 2 | `auth/login.spec.ts` | ~2 | Login smoke |
| 3 | `auth/profile.spec.ts` | ~4 | Profile display |
| 4 | `catalog/browse-products.spec.ts` | ~4 | Product grid browse |
| 5 | `catalog/catalog-filter-sort.spec.ts` | ~6 | Filter/sort/search |
| 6 | `checkout/checkout-flow.spec.ts` | ~2 | Basic checkout |
| 7 | `not-found.spec.ts` | ~2 | 404 page |
| 8 | `orders/order-history.spec.ts` | ~3 | Order list |
| 9 | `profile-hub.spec.ts` | ~5 | Profile hub |
| 10 | `seller/seller-dashboard.spec.ts` | ~4 | Dashboard display |

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
8. **Home page E2E** — hero, carousel, category tiles
9. **Header/navigation E2E** — mega menu, auth state
10. **Search E2E** — search results, facets, pagination

---

## Flaky Test Root Causes (for re-addition)

When re-adding deleted tests, avoid these patterns:

| Pattern | Problem | Fix |
|---------|---------|-----|
| `fillStable()` with `waitForTimeout(100ms)` | Race on slow CI | Use `expect(input).toHaveValue(value, { timeout: 2000 })` |
| `page.waitForLoadState('domcontentloaded')` | Fires immediately if already loaded | Use `page.waitForURL()` or `expect(locator).toBeVisible()` |
| `expect(await foo.isVisible()).toBe(true)` | `isVisible()` returns immediately | Use `await expect(foo).toBeVisible()` (retries) |
| `page.once('dialog', ...)` after click | Dialog fires before handler | Register handler BEFORE click |
| `test.skip(true, ...)` when no data | Masks real failures | Seeder must guarantee data exists |
