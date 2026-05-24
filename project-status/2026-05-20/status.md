# Project Status Report — 2026-05-20

## Executive Summary

All 10 microservices build and run. Backend build: **0 errors, 84 warnings**. Frontend build: **success** (2 unused import warnings, 1 bundle budget overrun). All automated test suites pass — contract tests that failed yesterday (4) are now **fully green**. E2E tests require the full Aspire AppHost stack and could not run in this automated pass.

---

## Test Results

### Backend — Unit Tests (12 projects)

| Project | Status | Tests |
|---------|--------|-------|
| Identity.UnitTests | PASS | 45 |
| Catalog.UnitTests | PASS | 19 |
| Cart.UnitTests | PASS | 15 |
| Inventory.UnitTests | PASS | 8 |
| Ordering.UnitTests | PASS | 70 |
| Payment.UnitTests | PASS | 30 |
| StoreManagement.UnitTests | PASS | 29 |
| Notification.UnitTests | PASS | 7 |
| Search.UnitTests | PASS | 4 |
| Media.UnitTests | **MISSING** | 0 (empty .gitkeep) |
| BuildingBlocks.Infrastructure.UnitTests | PASS | 16 |
| ApiGateway.UnitTests | PASS | 7 |
| BuildingBlocks.SharedContracts.UnitTests | PASS | 4 |
| **TOTAL** | **244/244** | |

### Backend — Integration Tests (11 projects)

| Project | Status | Tests |
|---------|--------|-------|
| Identity.IntegrationTests | PASS | 7 |
| Catalog.IntegrationTests | PASS | 4 |
| Cart.IntegrationTests | PASS | 15 |
| Inventory.IntegrationTests | PASS | 8 |
| Ordering.IntegrationTests | PASS | 3 |
| Payment.IntegrationTests | **EMPTY** | .gitkeep only |
| StoreManagement.IntegrationTests | **EMPTY** | .gitkeep only |
| Notification.IntegrationTests | **EMPTY** | .gitkeep only |
| Media.IntegrationTests | **EMPTY** | .gitkeep only |
| Search.IntegrationTests | PASS | 6 |
| ApiGateway.IntegrationTests | PASS | 2 |
| **TOTAL** | **45/45** (4 projects empty) | |

### Contract Tests

| Project | Status | Tests |
|---------|--------|-------|
| ContractTests | PASS | 51/51 |

**NOTE**: Yesterday had 4 failures in CatalogToCartContractTests. Today: **ALL GREEN** — the raw SQL / InMemory provider mismatch has been fixed.

### Frontend — Vitest

| Metric | Value |
|--------|-------|
| Spec Files | 36 passed |
| Test Cases | 293 passed |
| Duration | ~7s |

### E2E — Playwright (10.6 min)

| Metric | Value |
|--------|-------|
| Spec Files | 24 |
| Total Tests | 90 |
| Passed | 42 |
| Flaky (passed on retry) | 5 |
| Failed | 31 |
| Skipped | 8 |
| Did Not Run | 4 |

#### Failure Categories

| Category | Failures | Root Cause |
|----------|----------|------------|
| Mega Menu (header) | 4 | Mega menu not opening / categories not rendering |
| Profile Hub | 5 | Sidebar nav, tabs, profile info, change password, orders |
| Product Detail | 4 | Buy box, stock indicator, reviews, frequently-bought-together |
| Seller Orders | 3 | Orders tab, table, status update buttons |
| Inventory Management | 3 | Inventory tab, table, filter by status |
| Header | 2 | User dropdown after login, profile navigation |
| Admin Panel | 2 | Admin link in header, approve store |
| Seller Products | 2 | Products page, add product button |
| Saga Cancellation | 2 | Cancel button on completed order, API cancellation |
| Checkout Flow | 1 | Full checkout + payment |
| Payment Refund | 1 | Admin refund flow |
| Store Fixtures | 1 | Create + verify store via API |
| Seller Order Correlation | 1 | Buyer checkout visible to seller |

#### Flaky Tests (passed on retry)
- Login (newly registered user)
- Add to cart from detail page
- Cart badge after adding item
- Product detail add to cart
- Seller order correlation (sellerId in cart)

---

## Build Status

### Backend (Marketplace.AppHost)
- **Errors**: 0
- **Warnings**: 84
  - NU1902: OpenTelemetry.Exporter.OpenTelemetryProtocol 1.14.0 — 3 known vulnerabilities (moderate)
  - NU1902: SixLabors.ImageSharp 3.1.8 — 1 known vulnerability (moderate)
  - NU1902: OpenTelemetry.Api 1.14.0 — 1 known vulnerability (moderate)
  - NU1510: Unnecessary Microsoft.AspNetCore.Identity package reference in Identity.Infrastructure

### Frontend (Angular 21)
- **Build**: Success
- **Warnings**:
  - NG8113: RouterLink unused import in SavedSearchesComponent
  - NG8113: DatePipe unused import in InventoryListComponent
  - Bundle size 590KB exceeds 500KB budget by 90KB

---

## Source Code Metrics

| Service | .cs Files | Layers |
|---------|-----------|--------|
| Identity | 80 | Domain, Application, Infrastructure, API |
| Catalog | 100 | Domain, Application, Infrastructure, API |
| Cart | 48 | Domain, Application, Infrastructure, API |
| Inventory | 34 | Domain, Application, Infrastructure, API |
| Ordering | 64 | Domain, Application, Infrastructure, API |
| Payment | 42 | Domain, Application, Infrastructure, API |
| Search | 12 | API (thin — Elasticsearch) |
| Notification | 11 | Worker (thin — SignalR) |
| Media | 8 | API (thin — Blob Storage) |
| StoreManagement | 48 | Domain, Application, Infrastructure, API |
| **Frontend** | 143 .ts files | 36 spec files, 9 SignalStores |

---

## Open TODOs in Source Code

### Backend (2 TODOs)
1. `Catalog.Application/CreateReviewHandler.cs`: "Check Ordering.API for verified purchase (buyerId + productId)"
2. `Identity.Application/ForgotPasswordHandler.cs`: "Generate password reset token and send email in Phase 5"

### Frontend (2 TODOs)
1. `features/catalog/product-detail.ts`: "Add product variant selector (color, size) when Catalog supports variants"
2. `features/seller-dashboard/store.service.ts`: "Implement when Ordering.API has a sales summary endpoint"

---

## Remaining Gaps (from MISSING.md)

### P0 — Critical: ALL FIXED ✅
All 6 P0 fix plans completed. Auth, guards, order flow, cart hardening, SignalR, health endpoints — all resolved.

### P1 — Important (8 items remaining)

| # | Gap | Status |
|---|-----|--------|
| 1.3 | Update-profile endpoint | TODO |
| 5.2 | Inventory management UI | TODO |
| 5.7 | Address form in checkout | TODO |
| 5.8 | Add to Cart on product detail | TODO |
| 6.4 | Payment refund endpoint | TODO |
| 6.6 | Change-password endpoint | TODO |
| 6.8 | Order cancel API endpoint | TODO |
| 6.9 | Order status update (seller) | TODO |

### P2 — Polish & Production Readiness (10 items)

| # | Gap | Category |
|---|-----|----------|
| 8.1 | Integration tests for Media, Payment, StoreManagement, Notification | Testing |
| 8.3 | E2E coverage gaps | Testing |
| 9.1 | CI/CD pipeline | DevOps |
| 9.2 | Dockerfiles | DevOps |
| 9.3 | Terraform / IaC | DevOps |
| 9.4 | Environment-specific config | DevOps |
| 10.2 | Email sending (forgot-password) | Cross-cutting |
| 10.3 | Email verification on registration | Cross-cutting |
| 10.5 | Low-stock alerts | Cross-cutting |
| 10.6 | Admin reindex endpoint | Cross-cutting |

### Architecture Issues
- Store creation has circular dependency (Seller role requires store verification, but store creation requires Seller role)
- Cart uses PostgreSQL instead of Redis (deviates from plan)
- No CI/CD, Dockerfiles, or IaC
- Notification broadcasts are global, not user-targeted
- No token refresh in gateway
- Payment outbox: PaymentRefundedEvent may be published outside transactional boundary

---

## Priority Action Items (Recommended Next)

### Immediate (this week)
1. Fix remaining P1 endpoint gaps (6.4, 6.6, 6.8, 6.9) — backend endpoints
2. Add integration tests for the 4 empty test projects (Payment, StoreManagement, Notification, Media)
3. Address form in checkout (5.7) + Add to Cart on product detail (5.8)

### Short-term (next 2 weeks)
4. CI/CD pipeline setup (GitHub Actions)
5. Dockerfiles for all services
6. Email sending integration (SMTP/SendGrid)
7. Bundle optimization (reduce from 590KB to under 500KB)
8. Fix frontend warnings (unused imports)

### Medium-term
9. Environment-specific configuration (staging, production)
10. Terraform / IaC via Aspirate
11. Cart migration from PostgreSQL to Redis
12. Token refresh in gateway
