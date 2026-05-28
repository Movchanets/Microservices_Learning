# E2E Test Results — 2026-05-26

**Run**: `npx playwright test --reporter=list`
**Workers**: 8
**Duration**: 20.2s
**Environment**: Aspire AppHost (local)

---

## Summary

| Status | Count |
|--------|-------|
| ✅ Passed | 47 |
| ❌ Failed | 1 |
| ⚠️ Flaky (retried, passed) | 2 |
| ⏭️ Skipped | 6 |
| 🚫 Did not run | 7 |
| **Total** | **63** |

## Pass Rate: 47/50 executed = **94%**

---

## ✅ Passed (47)

| # | Test | Time |
|---|------|------|
| 1 | Catalog: Browse Products › should display product list on catalog page | 1.1s |
| 2 | Authentication: Login › should login successfully with newly registered user | 3.0s |
| 3 | Auth: Profile › should display user profile after login | 2.6s |
| 4 | Home Page › should display hero banner on load | 1.2s |
| 5 | Admin: Panel › should redirect unauthenticated from admin panel | 1.8s |
| 6 | 404 Not Found Page › should display 404 heading for unknown routes | 1.1s |
| 7 | Catalog: Browse Products › should navigate to product detail when clicking a product | 1.5s |
| 8 | Checkout: Order Flow › should show checkout page | 1.1s |
| 9 | Home Page › should display Shop by Category heading and category tiles | 1.1s |
| 10 | 404 Not Found Page › should have a working Go Home link | 1.1s |
| 11 | Checkout: Order Flow › should show empty cart message when no items | 1.3s |
| 12 | Admin: Panel › should show admin panel for admin users | 1.2s |
| 13 | Orders: Order History › should display orders page after login | 945ms |
| 14 | Home Page › should navigate to catalog when clicking a category tile | 1.2s |
| 15 | Catalog: Browse Products › should search for products | 1.2s |
| 16 | Authentication: Login › should show error with invalid credentials | 1.3s |
| 17 | User Profile Hub › should display profile hub with sidebar navigation | 936ms |
| 18 | Auth: Profile › should have logout button | 1.0s |
| 19 | Orders: Order History › should show empty state when no orders | 1.1s |
| 20 | Admin: Panel › should display users list | 2.2s |
| 21 | Home Page › should display deal of the day section when products exist | 1.1s |
| 22 | Auth: Profile › should redirect to login when not authenticated | 1.2s |
| 23 | Catalog: Filtering, Sorting & Pagination › should search and reduce product count | 1.0s |
| 24 | User Profile Hub › should navigate between profile tabs | 1.9s |
| 25 | Home Page › should display featured products carousel | 989ms |
| 26 | Seller: Dashboard › should redirect unauthenticated from seller dashboard | 888ms |
| 27 | Shared Layout: Header › should display logo, search bar, and cart button | 1.0s |
| 28 | Admin: Panel › should navigate to verifications tab | 953ms |
| 29 | User Profile Hub › should display user profile information | 1.0s |
| 30 | Home Page › should display new arrivals carousel | 969ms |
| 31 | Home Page › should navigate to product detail when clicking a featured product | 1.1s |
| 32 | Shared Layout: Header › should show Sign in link when not authenticated | 866ms |
| 33 | Seller: Dashboard › should show seller dashboard for seller users | 1.2s |
| 34 | Admin: Panel › should show admin link in header for admin users | 1.3s |
| 35 | User Profile Hub › should show change password section | 1.1s |
| 36 | Seller: Dashboard › should navigate to seller products | 1.0s |
| 37 | Admin: Panel › should NOT show admin link for non-admin users | 947ms |
| 38 | Home Page › should have working header navigation on home page | 812ms |
| 39 | User Profile Hub › should show order history on orders tab | 1.2s |
| 40 | Seller: Dashboard › should navigate to store settings | 1.1s |
| 41 | Shared Layout: Header › should navigate to home when clicking logo | 1.1s |
| 42 | Shared Layout: Header › should open and close mega menu | 1.2s |
| 43 | Shared Layout: Header › should open cart drawer when clicking cart button | 646ms |
| 44 | Shared Layout: Header › should close cart drawer | 1.0s |
| 45 | Shared Layout: Header (Authenticated) › should show user menu when authenticated | 667ms |
| 46 | Shared Layout: Header (Authenticated) › should open user dropdown and show profile link | 741ms |
| 47 | Shared Layout: Footer › should display theme toggle button | 547ms |
| 48 | Shared Layout: Footer › should toggle theme via dropdown | 782ms |

## ⚠️ Flaky — Passed on Retry (2)

| Test | Issue |
|------|-------|
| Authentication: Login › should login successfully with newly registered user | Submit button disabled on first attempt (race condition with form validation) |
| Catalog: Filtering, Sorting & Pagination › should show empty state for no-match search | Empty state not detected on first attempt (timing) |

## ❌ Failed (1)

| Test | Error |
|------|-------|
| Seller: Product & SKU CRUD › should create a product without SKUs | `Verify store failed: 403` — Store verification endpoint returns 403 (store not verified). **Unrelated to cart/checkout fix.** |

## ⏭️ Skipped (6)

All from `Catalog: Filtering, Sorting & Pagination` — category sidebar filter, sort, price range, pagination. Skipped due to missing category filter UI elements.

## 🚫 Did not run (7)

All from `Seller: Product & SKU CRUD` — blocked by the first test failure (store verification 403). Tests for: add SKUs, duplicate SKU, change price, remove SKU, activate product, delete product, minPrice/maxPrice list.

---

## Seeder Verification

The seeder (`Seeder.App`) ran successfully with the fix:

```
✓ Added PHONE-IPHONE-15-PRO × 1
✓ Added AUDIO-SONY-WH1000XM5 × 2
✓ Added BOOK-CLEANCODE × 3
Cart has 3 items, total: $2114.00
✓ Checkout accepted. CorrelationId: 81d0727d-fed0-43ca-b4ef-bbad3551b5a8
═══ Order Flow: Final status = Completed ═══
  Order ID:  81d0727d-fed0-43ca-b4ef-bbad3551b5a8
  Total:     $2114.00
  Status:    Completed
```

---

## Fix Impact

**Before fix**: `addToCart()` sent `{ productId, quantity }` → backend price lookup by `SkuId=Guid.Empty` failed → "SKU not found" 400 → cart empty → checkout "Cart is empty."

**After fix**: `addToCart()` sends `{ productId, skuId, skuCode, quantity }` → backend finds price → item added → checkout succeeds.

**Files changed**:
- `tests/E2ETests/utils/cart-helpers.ts` — E2E helper
- `src/Tools/Seeder.App/Models/Models.cs` — Added SkuResponseDto, ProductWithSkusDto
- `src/Tools/Seeder.App/Seeders/ProductSeeder.cs` — Returns (ProductId, SkuId)
- `src/Tools/Seeder.App/Worker.cs` — 3-tuple productIds
- `src/Tools/Seeder.App/Seeders/OrderFlowSeeder.cs` — Sends SkuId + SkuCode
