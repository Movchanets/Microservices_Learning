# Gaps & TODOs — 2026-05-20

## Test Coverage Gaps

### Empty Test Projects (need implementation)
- [ ] `tests/UnitTests/Media.UnitTests/` — only .gitkeep
- [ ] `tests/IntegrationTests/Payment.IntegrationTests/` — only .gitkeep
- [ ] `tests/IntegrationTests/StoreManagement.IntegrationTests/` — only .gitkeep
- [ ] `tests/IntegrationTests/Notification.IntegrationTests/` — only .gitkeep
- [ ] `tests/IntegrationTests/Media.IntegrationTests/` — only .gitkeep

### Frontend Specs Missing
- [ ] Reviews & Ratings components
- [ ] Search Facet components
- [ ] Product variant selector (when implemented)

---

## Backend TODOs

### Missing Endpoints
- [ ] `POST /api/identity/profile/update` — UpdateProfileCommand
- [ ] `POST /api/identity/change-password` — ChangePasswordCommand
- [ ] `POST /api/orders/{id}/cancel` — CancelOrderCommand (handler exists, no endpoint)
- [ ] `PUT /api/orders/{id}/status` — Order status update (seller marks shipped/completed)
- [ ] `POST /api/payments/{id}/refund` — Payment refund endpoint
- [ ] Single-item cart endpoints (add/update/remove individual items)

### Source Code TODOs
- [ ] `Catalog.Application/CreateReviewHandler.cs` — Check verified purchase in Ordering
- [ ] `Identity.Application/ForgotPasswordHandler.cs` — Implement email sending

### Architecture Fixes
- [ ] Payment outbox: route PaymentRefundedEvent through EF Core outbox (not in-memory bus)
- [ ] Cart repo retry: throw custom concurrency exception instead of falling through
- [ ] Payment GET /api/payments/order/{orderId}: add ownership check (buyerId vs JWT claim)
- [ ] Store creation circular dependency: need Seller role → need store verification

---

## Frontend TODOs

### Missing UI
- [ ] Address form in checkout page
- [ ] "Add to Cart" button on product detail page
- [ ] Inventory management page (seller)
- [ ] Product variant selector (color, size)
- [ ] Sales summary dashboard (needs backend endpoint)
- [ ] Store deletion UI
- [ ] Product media upload integration with Media.API

### Build Fixes
- [ ] Remove unused RouterLink import from SavedSearchesComponent
- [ ] Remove unused DatePipe import from InventoryListComponent
- [ ] Reduce bundle size from 590KB to under 500KB (lazy loading audit)

---

## DevOps TODOs

- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Dockerfiles for all services
- [ ] Terraform / IaC via Aspirate
- [ ] Environment-specific config (staging, production)
- [ ] Package vulnerability updates (OpenTelemetry 1.14.0, SixLabors.ImageSharp 3.1.8)

---

## Cross-cutting TODOs

- [ ] Email sending (SMTP/SendGrid) for forgot-password flow
- [ ] Email verification on registration
- [ ] Low-stock alerts in Inventory
- [ ] Admin Elasticsearch reindex endpoint
- [ ] User-targeted SignalR notifications (not broadcast)
- [ ] Token refresh in API Gateway
- [ ] Request/response logging middleware

---

## E2E Test Failures (31/90 — 2026-05-20)

### Critical (blocks core flows)
- [ ] `checkout-flow.spec.ts` — Full checkout + payment flow broken
- [ ] `store-fixtures.spec.ts` — Store creation/verification via API fails
- [ ] `saga-aware-cancellation.spec.ts` (2) — Order cancellation not working

### UI Components Not Rendering
- [ ] `header-mega-menu.spec.ts` (4) — Mega menu not opening, categories missing
- [ ] `profile-hub.spec.ts` (5) — Profile sidebar, tabs, info, password, orders
- [ ] `product-detail-enhanced.spec.ts` (4) — Buy box, stock indicator, reviews, frequently-bought-together
- [ ] `header.spec.ts` (2) — User dropdown, profile navigation

### Seller Dashboard Incomplete
- [ ] `seller-orders.spec.ts` (3) — Orders tab, table, status update buttons
- [ ] `seller/seller-products.spec.ts` (2) — Products page, add product button
- [ ] `inventory-management.spec.ts` (3) — Inventory tab, table, filter

### Admin Issues
- [ ] `admin/admin-panel.spec.ts` — Admin link not showing in header for admin users
- [ ] `admin/admin-store-detail.spec.ts` — Approve store via detail page

### Other
- [ ] `payment-refund.spec.ts` — Admin refund flow
- [ ] `seller-order-correlation.spec.ts` — Buyer checkout not visible to seller
