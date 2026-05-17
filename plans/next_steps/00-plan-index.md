# Next Steps — Implementation Plans

**Created:** 2026-05-16
**Purpose:** Independent, parallelizable implementation plans for marketplace features
**Based on:** `plans/future_design/` design documents + `plans/MISSING.md` gaps

---

## Plans

| # | Plan | Scope | Dependencies | Status | Vitest | Unit | Integration | E2E | POM |
|---|------|-------|--------------|--------|--------|------|-------------|-----|-----|
| 01 | [Global Header & Mega-Menu](01-global-header-mega-menu.md) | Frontend + Catalog.API tree endpoint | None | ✅ Verified | ✅ | ✅ | ❌ | ✅ | ✅ |
| 02 | [User Profile Hub](02-user-profile-hub.md) | Identity.API + Frontend profile | None | ✅ Verified | ✅ | ✅ | ❌ | ✅ | ✅ |
| 03 | [Cart & Checkout Optimization](03-cart-checkout-optimization.md) | Cart.API + Ordering.API + Frontend | None | ✅ Verified | ✅ | ✅ | ❌ | ✅ | ✅ |
| 04 | [Product Detail Enhancements](04-product-detail-enhancements.md) | Catalog.API + Inventory + Frontend | None | ✅ Verified | ✅ | ✅ | ❌ | ✅ | ✅ |
| 05 | [Reviews & Ratings](05-reviews-ratings.md) | Catalog.API + Media.API + Frontend | None | ✅ Verified | ❌ | ❌ | ❌ | ❌ | ❌ |
| 06 | [Homepage Content Blocks](06-homepage-content-blocks.md) | Catalog.API + Frontend | Plan 01 | ✅ Verified | ❌ | ❌ | ❌ | ❌ | ❌ |
| 07 | [Search & Discovery](07-search-discovery.md) | Search.API + Identity + Frontend | None | ✅ Verified | ❌ | ❌ | ❌ | ❌ | ❌ |
| 08 | [Inventory Management UI](08-inventory-management-ui.md) | Inventory.API + Frontend | None | ✅ Verified | ✅ | ✅ | ❌ | ✅ | ✅ |
| 09 | [Order Cancellation & Status](09-order-cancellation.md) | Ordering.API + Notification + Frontend | None | ✅ Verified | ✅ | ✅ | ❌ | ✅ | ✅ |

---

## Status: ALL PLANS VERIFIED (2026-05-16)

All 9 plans have been implemented and reviewed. Remaining gaps are tracked in `project_state/2026-05-16/todos-and-gaps.md`.
