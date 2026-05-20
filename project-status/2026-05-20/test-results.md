# Test Results — 2026-05-20

## Backend Unit Tests: 244/244 PASSING ✅

| Project | Passed | Failed | Duration |
|---------|--------|--------|----------|
| Identity.UnitTests | 45 | 0 | 335ms |
| Catalog.UnitTests | 19 | 0 | 106ms |
| Cart.UnitTests | 15 | 0 | 125ms |
| Inventory.UnitTests | 8 | 0 | 116ms |
| Ordering.UnitTests | 70 | 0 | 170ms |
| Payment.UnitTests | 30 | 0 | 135ms |
| StoreManagement.UnitTests | 29 | 0 | 101ms |
| Notification.UnitTests | 7 | 0 | 611ms |
| Search.UnitTests | 4 | 0 | 180ms |
| BuildingBlocks.Infrastructure.UnitTests | 16 | 0 | 56ms |
| ApiGateway.UnitTests | 7 | 0 | 64ms |
| BuildingBlocks.SharedContracts.UnitTests | 4 | 0 | 52ms |
| **TOTAL** | **244** | **0** | |

Media.UnitTests: MISSING (directory exists with .gitkeep only)

## Backend Integration Tests: 45/45 PASSING ✅

| Project | Passed | Failed | Duration |
|---------|--------|--------|----------|
| Identity.IntegrationTests | 7 | 0 | 829ms |
| Catalog.IntegrationTests | 4 | 0 | 1s |
| Cart.IntegrationTests | 15 | 0 | 1s |
| Inventory.IntegrationTests | 8 | 0 | 1s |
| Ordering.IntegrationTests | 3 | 0 | 2s |
| Search.IntegrationTests | 6 | 0 | 2s |
| ApiGateway.IntegrationTests | 2 | 0 | 225ms |
| **TOTAL** | **45** | **0** | |

Empty projects: Payment, StoreManagement, Notification, Media (all .gitkeep only)

## Contract Tests: 51/51 PASSING ✅

| Project | Passed | Failed | Duration |
|---------|--------|--------|----------|
| ContractTests | 51 | 0 | 4s |

NOTE: Yesterday had 4 failures (CatalogToCartContractTests — raw SQL vs InMemory). Fixed!

## Frontend Vitest: 293/293 PASSING ✅

| Metric | Value |
|--------|-------|
| Spec Files | 36 passed |
| Tests | 293 passed |
| Duration | 7.33s |

## E2E Playwright: 42/90 PASSED (31 failed, 5 flaky, 8 skipped, 4 did not run)

Run date: 2026-05-20
Duration: 10.6 min
BASE_URL: http://localhost:4201 (Aspire-managed)
Chromium, 1 worker, retries=1

### Passing Spec Files (all tests green)
- auth/forgot-password.spec.ts (4 tests)
- auth/profile.spec.ts (4 tests)
- cart-drawer.spec.ts (5 tests)
- catalog/browse-products.spec.ts (3 passed, 1 skipped)
- checkout/checkout-flow.spec.ts (2 tests)
- header.spec.ts (2 passed, 2 failed)
- order-cancellation.spec.ts (tests passing)
- orders/order-history.spec.ts (tests passing)

### Failing Spec Files
- admin/admin-panel.spec.ts — admin link in header for admin users
- admin/admin-store-detail.spec.ts — approve store via detail page
- cart/add-to-cart.spec.ts — add product from detail page (flaky)
- checkout-flow.spec.ts — full checkout flow
- header-mega-menu.spec.ts — 4 mega menu interactions
- inventory-management.spec.ts — 3 inventory UI tests
- payment-refund.spec.ts — admin refund
- product-detail-enhanced.spec.ts — 4 product detail tests
- profile-hub.spec.ts — 5 profile hub tests
- saga-aware-cancellation.spec.ts — 2 cancellation tests
- seller/seller-products.spec.ts — 2 seller product tests
- seller-order-correlation.spec.ts — buyer checkout visible to seller
- seller-orders.spec.ts — 3 seller order tests
- store-fixtures.spec.ts — create + verify store via API

### Root Cause Summary
Most failures fall into these categories:
1. UI elements not rendering (mega menu, profile hub, product detail components)
2. Seller/admin dashboard features incomplete (orders, inventory, products)
3. API-level failures (checkout flow, store fixtures, payment refund)
4. Timing/flaky issues (login, cart badge — pass on retry)
