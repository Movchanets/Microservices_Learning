# E2E Test Inventory (Playwright)

**Project:** Marketplace Microservices
**Framework:** Playwright (TypeScript)
**Last Updated:** 2026-06-19
**Total:** 17 spec files, ~63 tests
**Config:** `tests/E2ETests/playwright.config.ts`
**Base URL:** `http://localhost:4201` (default)

---

## Current E2E Test Files

| # | Test File | Tests | Primary Feature | Auth Required |
|---|-----------|-------|-----------------|---------------|
| 1 | `admin/admin-panel.spec.ts` | 6 | admin | ✅ Admin |
| 2 | `auth/login.spec.ts` | 2 | auth | ❌ |
| 3 | `auth/profile.spec.ts` | 3 | auth | ✅ Buyer/Admin |
| 4 | `catalog/browse-products.spec.ts` | 4 | catalog | ❌ |
| 5 | `catalog/catalog-filter-sort.spec.ts` | 6 | catalog | ❌ |
| 6 | `checkout/checkout-flow.spec.ts` | 2 | checkout | ✅ Buyer |
| 7 | `home/home-page.spec.ts` | 8 | home | ❌ |
| 8 | `not-found.spec.ts` | 2 | shared | ❌ |
| 9 | `orders/order-history.spec.ts` | 3 | orders | ✅ Buyer |
| 10 | `profile-hub.spec.ts` | 5 | auth | ✅ Buyer |
| 11 | `seller/seller-dashboard.spec.ts` | 4 | seller | ✅ Seller |
| 12 | `seller/product-sku-crud.spec.ts` | 8 | seller | ✅ Seller/Admin |
| 13 | `shared/layout.spec.ts` | 10 | shared | Mixed |
| | **Total** | **~63** | | |

---

## Test Details

### admin/admin-panel.spec.ts (6 tests)

| Test | Description |
|------|-------------|
| Redirect unauthenticated | Verifies /admin redirects to /login for unauthenticated users |
| Show admin panel for admin | Admin panel displays heading, Users link, Verifications link |
| Display users list | Users table has rows |
| Navigate to verifications | Verifications tab loads |
| Show admin link in header | Admin nav link visible for admin users |
| Hide admin link for non-admin | Admin nav link hidden for seller users |

### auth/login.spec.ts (2 tests)

| Test | Description |
|------|-------------|
| Login with new user | Register via API → clear session → login → verify redirect to /catalog |
| Invalid credentials error | Login with wrong password → verify 401 error |

### auth/profile.spec.ts (3 tests)

| Test | Description |
|------|-------------|
| Display profile after login | User email visible on /profile |
| Logout button present | Sign out button visible |
| Redirect when not authenticated | /profile redirects to /auth/login |

### catalog/browse-products.spec.ts (4 tests)

| Test | Description |
|------|-------------|
| Product list on catalog page | Catalog title visible, product cards > 0 |
| Navigate to product detail | Click product → URL matches /catalog/:id |
| Search for products | Search "iPhone" → product count decreases |
| Filter by category | Click category button → products filtered |

### catalog/catalog-filter-sort.spec.ts (6 tests)

| Test | Description |
|------|-------------|
| Filter by category sidebar | Sidebar filter → product count changes |
| Sort by price | Sort dropdown → products reordered |
| Filter by price range | Price range facet → products filtered |
| Paginate through pages | Page 2 → products displayed |
| Search and reduce count | Search term → fewer products |
| Empty state for no-match | Gibberish search → empty state shown |

### checkout/checkout-flow.spec.ts (2 tests)

| Test | Description |
|------|-------------|
| Show checkout page | /checkout displays "Checkout" heading |
| Empty cart message | Cart page with no items → empty message or hidden confirm button |

### home/home-page.spec.ts (8 tests)

| Test | Description |
|------|-------------|
| Hero banner visible | Hero banner displayed on load |
| Category tiles | "Shop by Category" heading + tiles rendered |
| Category tile navigation | Click tile → navigate to /catalog |
| Deal of the day | Deal section visible when featured products exist |
| Featured products carousel | Carousel visible with products |
| New arrivals carousel | New arrivals section visible |
| Featured product navigation | Click featured product → /catalog/:id |
| Header navigation | Logo + cart button visible on home page |

### not-found.spec.ts (2 tests)

| Test | Description |
|------|-------------|
| 404 heading display | Unknown route → 404 heading + message visible |
| Go Home link | Click "Go Home" → navigate to /home |

### orders/order-history.spec.ts (3 tests)

| Test | Description |
|------|-------------|
| Orders page after login | "My Orders" heading visible |
| Empty state | No orders → empty message or heading shown |
| Navigate to order detail | Click order link → /orders/:id |

### profile-hub.spec.ts (5 tests)

| Test | Description |
|------|-------------|
| Profile hub display | Sidebar navigation visible |
| Tab navigation | Orders tab → Settings tab → correct URLs |
| Profile information | Settings tab shows "Profile Information" heading |
| Change password section | "Change Password" heading visible |
| Order history on orders tab | Orders tab shows count or empty message |

### seller/seller-dashboard.spec.ts (4 tests)

| Test | Description |
|------|-------------|
| Redirect unauthenticated | /seller redirects to /login |
| Dashboard for sellers | Heading, Products/Orders/Settings links visible |
| Navigate to seller products | /seller/products loads |
| Navigate to store settings | /seller/settings → "Store Settings" heading |

### seller/product-sku-crud.spec.ts (8 tests)

| Test | Description |
|------|-------------|
| Create product without SKUs | API: create product → verify empty SKUs |
| Add multiple SKUs | API: add 2 SKUs → verify both present |
| Reject duplicate SKU code | API: duplicate SKU → 400 error |
| Change SKU price | API: PATCH price → verify updated |
| Remove a SKU | API: delete SKU → verify removed |
| Activate product | API: activate with SKUs → status "Active" |
| Delete product | API: delete → verify gone (404/null) |
| List price aggregation | API: products list → minPrice/maxPrice/skuCount |

### shared/layout.spec.ts (10 tests)

| Test | Description |
|------|-------------|
| Logo, search, cart visible | Header elements on home page |
| Sign in link when unauthenticated | Login link visible |
| Logo navigates to home | Click logo from /catalog → /home |
| Mega menu open/close | Toggle mega menu → visible/hidden |
| Cart drawer open | Click cart → drawer opens |
| Cart drawer close | Open drawer → close → drawer hidden |
| User menu when authenticated | Auth user → user menu trigger visible |
| User dropdown profile link | Open user menu → profile + logout links visible |
| Theme toggle button | Footer theme button visible |
| Theme toggle dropdown | Toggle dark/light → html class changes |

---

## Deleted E2E Tests (21 files, ~94 tests)

These were removed during flaky test cleanup.

| Deleted File | Tests | Feature | Reason |
|-------------|-------|---------|--------|
| `auth/forgot-password.spec.ts` | 4 | auth | Flaky timing |
| `cart/add-to-cart.spec.ts` | 3 | cart | Flaky fillStable timing |
| `cart/cart-drawer.spec.ts` | 5 | cart | Drawer open/close race |
| `checkout/checkout-edge-cases.spec.ts` | 5 | checkout | Address form fill flaky |
| `root/checkout-flow.spec.ts` | 2 | checkout | Duplicate |
| `root/header.spec.ts` | 3 | shared | Auth state display flaky |
| `root/header-mega-menu.spec.ts` | 6 | shared | Mega menu hover timing |
| `root/inventory-management.spec.ts` | 4 | seller | Table load timing |
| `root/order-cancellation.spec.ts` | 5 | orders | Dialog handler race |
| `root/payment-refund.spec.ts` | 5 | admin | Refund dialog timing |
| `root/product-detail-enhanced.spec.ts` | 6 | catalog | Buy box interaction flaky |
| `root/profile-hub.spec.ts` | 5 | auth | Tab navigation flaky |
| `root/saga-aware-cancellation.spec.ts` | 3 | orders | Saga completion wait |
| `root/seller-order-correlation.spec.ts` | 2 | seller | Correlation display flaky |
| `root/seller-orders.spec.ts` | 4 | seller | Order status update flaky |
| `root/store-fixtures.spec.ts` | 5 | stores | Infrastructure, not feature tests |
| `admin/admin-store-detail.spec.ts` | 2 | admin | Store approval dialog flaky |
| `admin/admin-user-management.spec.ts` | 5 | admin | Role change dialog flaky |
| `seller/seller-product-crud.spec.ts` | 5 | seller | Product form fill flaky |
| `seller/seller-products.spec.ts` | 3 | seller | Product list load flaky |
| `seller/store-settings-crud.spec.ts` | 4 | seller | Settings form fill flaky |

---

## Flaky Test Root Causes (Avoid When Re-Adding)

| Pattern | Problem | Fix |
|---------|---------|-----|
| `fillStable()` with `waitForTimeout(100ms)` | Race on slow CI | Use `expect(input).toHaveValue(value, { timeout: 2000 })` |
| `page.waitForLoadState('domcontentloaded')` | Fires immediately if already loaded | Use `page.waitForURL()` or `expect(locator).toBeVisible()` |
| `expect(await foo.isVisible()).toBe(true)` | `isVisible()` returns immediately | Use `await expect(foo).toBeVisible()` (retries) |
| `page.once('dialog', ...)` after click | Dialog fires before handler | Register handler BEFORE click |
| `test.skip(true, ...)` when no data | Masks real failures | Seeder must guarantee data exists |

---

## Re-Addition Priorities

### P0 — Must re-add (revenue/security critical)

1. **Cart E2E** — add-to-cart, cart-drawer, anonymous cart, merge
2. **Checkout E2E** — payment processing, edge cases, confirmation
3. **Registration E2E** — entire signup flow

### P1 — Should re-add

4. **Seller dashboard E2E** — product CRUD, settings, orders (UI-level)
5. **Admin E2E** — user management, store detail, refunds
6. **Ordering E2E** — cancellation, saga flow

### P2 — Nice to have

7. **Search E2E** — search results, facets, pagination
8. **Store browsing E2E** — store page, store search

---

## Playwright Configuration

| Setting | Value |
|---------|-------|
| Test Directory | `tests/E2ETests/tests/` |
| Workers (CI) | 3 |
| Retries | 1 |
| Timeout | 60s |
| Expect Timeout | 10s |
| Action Timeout | 15s |
| Navigation Timeout | 30s |
| Screenshots | On first retry |
| Video | On first retry |
| Trace | On first retry |
| Browser | Chromium only |

---

## How to Run

```bash
# All E2E tests (requires running app)
cd tests/E2ETests && npx playwright test

# Specific file
cd tests/E2ETests && npx playwright test tests/admin/admin-panel.spec.ts

# With UI
cd tests/E2ETests && npx playwright test --ui

# Local config (different base URL)
cd tests/E2ETests && npx playwright test --config=playwright.local.config.ts
```

---

*Generated from spec files in `tests/E2ETests/tests/`.*
