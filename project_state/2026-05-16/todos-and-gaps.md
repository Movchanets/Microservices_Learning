# TODOs and Gaps — 2026-05-16 (Updated after Plans 01-09)

**Last updated:** 2026-05-16 after reviewing all 9 implementation plans

---

## Plans 01-09 Status

| Plan | Name | Status | Remaining Gaps |
|------|------|--------|----------------|
| 01 | Global Header & Mega-Menu | ✅ Complete | None |
| 02 | User Profile Hub | ✅ Complete | None |
| 03 | Cart & Checkout Optimization | ✅ Complete | None |
| 04 | Product Detail Enhancements | ✅ Complete | Stock badge uses status not inventory (cosmetic) |
| 05 | Reviews & Ratings | ✅ Complete | No photo upload, no tests, GetSummaryAsync in-memory |
| 06 | Homepage Content Blocks | ✅ Complete | Recently Viewed shows count only |
| 07 | Search & Discovery | ✅ Verified | avg_rating aggregation, active filter chips, PriceAlertConsumer, breadcrumbs dropdowns |
| 08 | Inventory Management UI | ✅ Complete | None |
| 09 | Order Cancellation & Status | ✅ Complete | None |

---

## Unimplemented / Mocked Tasks from Plans 01-09

### HIGH — Missing Features

| # | Plan | Item | Description |
|---|------|------|-------------|
| 1 | 05 | Photo upload in write-review | Media.API integration for review photos (max 5 images) |
| 2 | 07 | PriceAlertConsumer | Notification.Worker consumer to check saved searches against product updates and push SignalR alerts |
| 3 | 07 | Active filter chips | Show active filters as removable chips in search-facets component |
| 4 | 07 | Breadcrumbs sibling dropdowns | Hovering a breadcrumb node shows dropdown of sibling categories |

### MEDIUM — Performance / Quality

| # | Plan | Item | Description |
|---|------|------|-------------|
| 5 | 05 | GetSummaryAsync optimization | Loads all reviews into memory; should use SQL aggregation (`GROUP BY Rating`, `AVG`, `COUNT`) |
| 6 | 07 | avg_rating aggregation | Elasticsearch `AverageAggregation` for `avg_rating` facet missing from search service |
| 7 | 05 | Backend tests for reviews | No unit tests for CreateReviewHandler, VoteReviewHandler, SellerResponseHandler |
| 8 | 05 | Frontend tests for reviews | No spec files for review-summary, review-list, write-review, review store |
| 9 | 07 | Frontend tests for search | No spec files for search-facets, search-bar, breadcrumbs, saved-searches |

### LOW — Cosmetic / Style

| # | Plan | Item | Description |
|---|------|------|-------------|
| 10 | 04 | `standalone: true` on product-card.ts | Redundant in Angular v20+ (default) |
| 11 | 04 | Stock badge based on status | Shows "In Stock" when `status === 'Active'`, not actual inventory |
| 12 | 09 | `standalone: true` on status-badge.ts | Redundant in Angular v20+ (default) |
| 13 | 06 | Recently Viewed product cards | Shows count only ("X items"), not actual product cards |

---

## Pre-existing Gaps (from before Plans 01-09)

### Backend TODOs

| Service | TODO | Priority | Status |
|---------|------|----------|--------|
| Identity.API | Email sending for forgot-password | P2 | Not started |
| Identity.API | Email verification on registration | P2 | Not started |
| Cart.API | Redis implementation (currently PostgreSQL) | P2 | Architectural deviation |
| Payment.API | Refund endpoint | P1 | Not started |
| Payment.API | Payment method selection | P2 | Only simulated |
| Search.API | Admin reindex endpoint | P2 | Not started |
| StoreManagement.API | Store deletion endpoint | P2 | Not started |
| Notification.Worker | Targeted notifications (not broadcast) | P2 | Broadcasts to all |
| API Gateway | Token refresh | P2 | No automatic token renewal |

### Frontend TODOs

| Feature | TODO | Priority | Status |
|---------|------|----------|--------|
| Cart | Remove x-buyer-id header pattern | P2 | Legacy code |
| Checkout | Payment method selection | P2 | Only simulated |
| Checkout | Express checkout (Apple Pay, Google Pay) | P2 | Not started |
| Checkout | Free shipping progress bar | P2 | Not started |
| Seller Dashboard | Media upload in product form | P2 | No image upload |
| Seller Dashboard | Sales summary endpoint | P2 | Currently hardcoded zeros |
| Seller Dashboard | SellerOrdersComponent bypasses store | P2 | Architectural inconsistency |
| Auth | Email verification flow | P2 | Unverified registrations |
| Catalog | Product variant selector | P2 | No color/size selection |

### Testing Gaps

| Gap | Status |
|-----|--------|
| Media.UnitTests | ❌ Empty project |
| Media.IntegrationTests | ❌ Empty |
| Notification.IntegrationTests | ❌ Empty |
| Ordering.IntegrationTests | ❌ Empty |
| Payment.IntegrationTests | ❌ Empty |
| StoreManagement.IntegrationTests | ❌ Empty |
| Full E2E checkout flow | ❌ Only page load check |
| E2E payment flow | ❌ Missing |
| E2E order creation flow | ❌ Missing |
| api-helpers.ts / db-helpers.ts | ⚠️ Stubs (empty methods) |

### DevOps

| Gap | Status |
|-----|--------|
| CI/CD pipeline | ❌ No GitHub Actions config |
| Dockerfiles | ❌ Aspire handles local only |
| Terraform / IaC | ❌ No infrastructure code |
| Environment-specific config | ❌ Only appsettings.json |

---

## Priority Summary

### Remaining P1 (from pre-existing, not covered by plans)
1. Refund endpoint (Payment.API)

### Remaining P2 (from pre-existing + plan gaps)
1. Photo upload in write-review (Plan 05)
2. PriceAlertConsumer (Plan 07)
3. Active filter chips (Plan 07)
4. Breadcrumbs sibling dropdowns (Plan 07)
5. GetSummaryAsync SQL optimization (Plan 05)
6. avg_rating aggregation (Plan 07)
7. Email sending / verification (Identity)
8. Cart Redis implementation
9. Payment method selection
10. Express checkout
11. Media upload in product form
12. Sales summary endpoint
13. Targeted notifications
14. Token refresh
15. Product variant selector
16. Admin reindex endpoint
17. Store deletion endpoint
18. Free shipping progress bar

### Deferred
1. CI/CD pipeline
2. Dockerfiles
3. Terraform / IaC
4. Environment-specific config
5. 5 empty integration test projects
6. 3 E2E test flows
