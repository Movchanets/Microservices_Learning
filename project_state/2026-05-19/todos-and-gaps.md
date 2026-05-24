# TODOs and Gaps — 2026-05-19

**Last updated:** 2026-05-19 (daily status check)

---

## Build & Test Status

| Component | Status | Details |
|-----------|--------|---------|
| Backend (Marketplace.slnx) | ✅ 0 errors | 154 warnings (mostly ASPDEPR002 `WithOpenApi` obsolete, CS9264 nullable) |
| Frontend (Angular) | ✅ Builds | 1 budget warning (589KB > 500KB limit) |
| Unit Tests | ✅ 254 passed | 0 failed across 11 test projects |
| Integration Tests | ✅ 44 passed | 7 test projects all green (incl. Ordering saga 3 tests) |
| Contract Tests | ⚠️ 47/51 | 4 FAILED — CatalogToCartContractTests (see below) |
| Frontend Vitest | ✅ 293 passed | 36 spec files, 0 failures |
| E2E (Playwright) | ⚠️ Not run | 24 spec files exist, requires running Aspire services |

---

## CRITICAL: 4 Failing Contract Tests

**Root cause:** `ProductPriceRepository.UpsertAsync()` calls `ExecuteSqlRawAsync()` which requires a relational DB provider, but contract tests use InMemory provider.

| Test | Error |
|------|-------|
| `ProductCreatedEvent_Contract_ShouldCreateProductPriceInCart` | `InvalidOperationException: Relational-specific methods can only be used when the context is using a relational database provider` |
| `ProductCreatedEvent_Contract_ShouldBeIdempotent` | Same |
| `ProductUpdatedEvent_Contract_ShouldCreateIfNotExists` | Same |
| `ProductUpdatedEvent_Contract_ShouldUpdateProductPriceInCart` | Same |

**Fix options:**
1. Replace `ExecuteSqlRawAsync` with pure EF Core upsert (preferred per user's "NO RAW SQL" rule)
2. Use SQLite in-memory (relational) for contract tests instead of EF InMemory
3. Mock the repository in contract tests

---

## Plans 01-16 Status

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
| 10 | Seller Order Correlation | ✅ Complete | None |
| 11 | Saga-Aware Cancellation | ✅ Implemented | CorrelatedBy<Guid> on CancelOrderEvent (minor) |
| 12 | Payment Refund | ✅ Complete | 30 unit tests pass |
| 13 | Search Integration Fix | ✅ Complete | ES 9.x container required |
| 14 | SignalR Hub Auth | ✅ Complete | — |
| 15 | E2E Test Remediation | ✅ Complete | 90 tests, 24 files, auth fixture, waitForTimeout removed |
| 16 | Code Review Fixes | ✅ Complete | TOCTOU, buyer=admin, saga swallowing fixed |

---

## Backend TODOs (by priority)

### P0 — Blocks Core Flow

| # | Service | Issue | Status |
|---|---------|-------|--------|
| 1 | ContractTests | 4 CatalogToCart tests fail (ExecuteSqlRawAsync on InMemory) | OPEN |

### P1 — Important Features

| # | Service | Issue | Status |
|---|---------|-------|--------|
| 2 | Payment.API | `PaymentRefundedEvent` published outside outbox | Deferred |
| 3 | Payment.API | `GET /api/payments/order/{orderId}` has no ownership check | Deferred |
| 4 | Cart | Retry loop falls through on exhaustion in CartRepository | Deferred |

### P2 — Polish & Production

| # | Service | Issue | Status |
|---|---------|-------|--------|
| 5 | Identity.API | Email sending for forgot-password | Not started |
| 6 | Identity.API | Email verification on registration | Not started |
| 7 | Cart.API | Redis implementation (currently PostgreSQL) | Architectural deviation |
| 8 | Payment.API | Payment method selection | Only simulated |
| 9 | Search.API | Admin reindex endpoint | Not started |
| 10 | StoreManagement.API | Store deletion endpoint | Not started |
| 11 | Notification.Worker | Targeted notifications (not broadcast) | Broadcasts to all |
| 12 | API Gateway | Token refresh | No automatic token renewal |
| 13 | All services | `WithOpenApi()` deprecation (ASPDEPR002) | 154 warnings, needs migration |
| 14 | Search.API | `NumberRange` deprecated → use `Number()` | 2 call sites |
| 15 | Identity.Application | `RefreshTokenHandler` has unread parameters | Warning CS9113 |

### Warnings from Plan 16 (Deferred)

| ID | Issue | Severity |
|----|-------|----------|
| W2 | Saga sends TransactionId=Guid.Empty in refund command | Low |
| W4 | ProductUpdatedConsumer hardcodes "USD" currency | Low |
| W6 | GET /api/payments/order/{orderId} has no ownership check | Medium |
| W7 | BuyerIdUserIdProvider query string fallback | Low |
| W8 | Jwt:Secret! null-forgiving operator | Low |
| W9 | 3 waitForTimeout remnants in page objects (100-150ms each) | Low |
| W10 | payment-refund.spec.ts sequential dependencies | Low |

### Suggestions from Plan 16 (Deferred)

| ID | Suggestion |
|----|-----------|
| S2 | Add composite index (TransactionId, Status) on Refunds |
| S3 | Align ES memory settings across test/AppHost |
| S4 | api-helpers.ts hardcodes baseUrl |
| S5 | Extract createAuthenticatedContext helper |
| S6 | Store TransactionId in saga state |
| S7 | Add IdempotencyKey to Refund |
| S8 | Add negative tests for RefundPaymentConsumer |
| S9 | Publish RefundFailedEvent on consumer failure |
| S10 | Remove dead db-helpers.ts |

---

## Frontend TODOs

### P2 — Features

| # | Feature | Issue | Status |
|---|---------|-------|--------|
| 1 | Cart | Remove x-buyer-id header pattern | Legacy code |
| 2 | Checkout | Payment method selection | Only simulated |
| 3 | Checkout | Express checkout (Apple Pay, Google Pay) | Not started |
| 4 | Checkout | Free shipping progress bar | Not started |
| 5 | Seller Dashboard | Media upload in product form | No image upload |
| 6 | Seller Dashboard | Sales summary endpoint | Currently hardcoded zeros |
| 7 | Auth | Email verification flow | Unverified registrations |
| 8 | Catalog | Product variant selector | No color/size selection |
| 9 | Bundle size | 589KB > 500KB budget | Needs lazy loading optimization |

### P2 — UX Polish

| # | Plan | Item | Description |
|---|------|------|-------------|
| 10 | 05 | Photo upload in write-review | Media.API integration for review photos (max 5 images) |
| 11 | 07 | PriceAlertConsumer | Notification.Worker consumer for saved search alerts |
| 12 | 07 | Active filter chips | Removable chips in search-facets component |
| 13 | 07 | Breadcrumbs sibling dropdowns | Hover dropdown of sibling categories |
| 14 | 04 | `standalone: true` on product-card.ts | Redundant in Angular v20+ (default) |
| 15 | 04 | Stock badge based on status | Shows "In Stock" when status === 'Active', not actual inventory |
| 16 | 09 | `standalone: true` on status-badge.ts | Redundant in Angular v20+ (default) |
| 17 | 06 | Recently Viewed product cards | Shows count only, not actual product cards |

---

## Testing Gaps

### Unit Tests — Coverage

| Project | Tests | Status |
|---------|-------|--------|
| Identity.UnitTests | 45 | ✅ |
| Ordering.UnitTests | 70 | ✅ |
| Payment.UnitTests | 30 | ✅ |
| StoreManagement.UnitTests | 29 | ✅ |
| Catalog.UnitTests | 19 | ✅ |
| Cart.UnitTests | 15 | ✅ |
| BuildingBlocks.Infrastructure | 16 | ✅ |
| BuildingBlocks.SharedContracts | 4 | ✅ |
| Inventory.UnitTests | 8 | ✅ |
| Notification.UnitTests | 7 | ✅ |
| Search.UnitTests | 4 | ✅ |
| ApiGateway.UnitTests | 7 | ✅ |
| **Total** | **254** | ✅ All pass |

### Integration Tests — Coverage

| Project | Tests | Status |
|---------|-------|--------|
| Cart.IntegrationTests | 14 | ✅ |
| Identity.IntegrationTests | 7 | ✅ |
| Inventory.IntegrationTests | 8 | ✅ |
| Search.IntegrationTests | 6 | ✅ |
| Ordering.IntegrationTests | 3 | ✅ (saga tests) |
| Catalog.IntegrationTests | 4 | ✅ |
| ApiGateway.IntegrationTests | 2 | ✅ |
| **Total** | **44** | ✅ All pass |

### Missing Tests

| Gap | Status |
|-----|--------|
| Media.UnitTests | ❌ Empty project |
| Media.IntegrationTests | ❌ Empty |
| Notification.IntegrationTests | ❌ Empty |
| Payment.IntegrationTests | ❌ Empty |
| StoreManagement.IntegrationTests | ❌ Empty |
| Ordering.IntegrationTests | ⚠️ Only 3 saga tests, no endpoint tests |
| Review handlers (CreateReview, VoteReview, SellerResponse) | ❌ No unit tests |
| Frontend: review components (4 specs) | ❌ No spec files |
| Frontend: search components (4 specs) | ❌ No spec files |
| ContractTests | ⚠️ 4 failing (see Critical section) |

### E2E Tests

| Spec | Status |
|------|--------|
| 24 spec files exist | ✅ Created in Plan 15 |
| Auth fixture | ✅ Implemented |
| waitForTimeout eliminated | ✅ (3 remnants 100-150ms in page objects) |
| Full E2E run | ⚠️ Not verified today (needs Aspire running) |

---

## DevOps Gaps (All Deferred)

| Gap | Status |
|-----|--------|
| CI/CD pipeline (GitHub Actions) | ❌ No config |
| Dockerfiles | ❌ Aspire handles local only |
| Terraform / IaC | ❌ No infrastructure code |
| Environment-specific config | ❌ Only appsettings.json |

---

## Priority Summary

### Immediate (blocks correctness)
1. **Fix 4 failing contract tests** — Replace `ExecuteSqlRawAsync` with pure EF Core upsert (aligns with "NO RAW SQL" rule)

### P1 — Next sprint
1. Payment outbox for `PaymentRefundedEvent`
2. Payment endpoint ownership check
3. Cart retry loop fallthrough fix

### P2 — Backlog
1. Email sending / verification (Identity)
2. Cart Redis migration
3. Payment method selection
4. Bundle size optimization (589KB → <500KB)
5. 5 empty integration test projects
6. Review handler unit tests
7. Search/review frontend specs
8. `WithOpenApi()` deprecation migration (154 warnings)
9. DevOps (CI/CD, Docker, Terraform)
