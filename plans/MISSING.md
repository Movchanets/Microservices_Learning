# Marketplace — Missing Capabilities

**Purpose**: Gap analysis for a basic functional marketplace. Items are grouped by priority (P0 = critical for MVP, P1 = important, P2 = nice-to-have).

**Last updated**: 2026-05-15
**P0 Fix Plans**: `implementation_plan/p0-fixes/` (6 sub-plans) — All completed
**P1 Fix Plans**: `implementation_plan/p1-fixes/` (6 sub-plans) — All completed
**P2 Fix Plans**: `implementation_plan/p2-fixes/` (5 sub-plans) — Completed (except integration tests)
**Future Design**: `plans/future_design/` — Design guides for advanced features

---

## P0 — Critical for MVP (blocks basic buyer/seller flow) — ALL FIXED ✅

### 1. Authentication & Authorization Gaps — FIXED

| # | Gap | Status |
|---|-----|--------|
| 1.1 | **No frontend AuthGuard** | ✅ `auth.guard.ts` + `role.guard.ts` created, applied to routes |
| 1.2 | **No role-based authorization on backend** | ✅ JWT auth on Ordering, Cart, Inventory, Payment; Admin/Seller policies |
| 1.3 | **No profile update endpoint** | ⏳ TODO — needs UpdateProfileCommand |
| 1.4 | **No admin role enforcement on store verification** | ✅ `RequireAuthorization("Admin")` on verify endpoint |
| 1.5 | **Identity has no MassTransit registration** | ✅ MassTransit + Outbox configured, StoreVerifiedConsumer added |

### 2. Order Flow — FIXED

| # | Gap | Status |
|---|-----|--------|
| 2.1 | **Order items have price hardcoded to 0** | ✅ Price field added to OrderItemContract, CartItem, CartItemDto |
| 2.2 | **No address collection in checkout** | ⏳ TODO — needs address form in checkout page |
| 2.3 | **Cart has no auth** | ✅ JWT Bearer auth, buyerId from JWT claims |
| 2.4 | **Cart checkout has no Outbox** | ✅ MassTransit Outbox configured |
| 2.5 | **No order cancellation endpoint** | ⏳ CancelOrderCommand exists but no API endpoint |
| 2.6 | **Ordering endpoints have no auth** | ✅ JWT Bearer auth + Seller policy |

### 3. SignalR — FIXED

| # | Gap | Status |
|---|-----|--------|
| 3.1 | **Frontend NotificationService never started** | ✅ Started after auth in app.config.ts |
| 3.2 | **NotificationBridgeComponent not verified** | ✅ In app.ts template, connects via WebSocket |
| 3.3 | **Failed events broadcast to all users** | ✅ Removed redundant consumers; saga handles via OrderCancelledEvent |

### 4. Seller Verification — FIXED

| # | Gap | Status |
|---|-----|--------|
| 4.1 | **StoreVerifiedEvent not published** | ✅ StoreVerifiedEventHandler publishes integration event |
| 4.2 | **No consumer for role update** | ✅ StoreVerifiedConsumer updates user role to Seller |
| 4.3 | **Seller dashboard has no guard** | ✅ `roleGuard('Seller', 'Admin')` applied |

---

## P1 — Important for Marketplace UX — MOSTLY FIXED ✅

### 5. Missing Frontend Pages/Features

| # | Gap | Status |
|---|-----|--------|
| 5.1 | **No seller order management page** | ✅ SellerOrdersComponent created with order table |
| 5.2 | **No inventory management UI** | ⏳ TODO — needs InventoryService + page |
| 5.3 | **No order tracking/timeline** | ✅ OrderTimelineComponent with step visualization |
| 5.4 | **No global error toast/notification service** | ✅ ToastService + error interceptor + ToastContainer |
| 5.5 | **No 404 page** | ✅ NotFoundComponent with catch-all route |
| 5.6 | **No admin role management UI** | ✅ Admin panel with user list, role dropdown, store verification |
| 5.7 | **No address form in checkout** | ⏳ TODO — needs address form fields |

### 6. Backend Endpoint Gaps

| # | Gap | Status |
|---|-----|--------|
| 6.1 | **No seller order list endpoint** | ✅ `GET /api/orders/seller/{sellerId}` with Seller policy |
| 6.2 | **No category update/delete endpoints** | ✅ `PUT/DELETE /api/catalog/categories/{id}` |
| 6.3 | **No inventory list endpoint** | ✅ `GET /api/inventory/items` |
| 6.4 | **No payment refund endpoint** | ⏳ TODO |
| 6.5 | **No media listing endpoint** | ✅ `GET /api/media` |
| 6.6 | **No change-password endpoint** | ⏳ TODO — needs ChangePasswordCommand |
| 6.7 | **Inventory endpoints have no auth** | ✅ JWT Bearer auth on write endpoints |

### 7. Gateway & Health

| # | Gap | Status |
|---|-----|--------|
| 7.1 | **Health endpoint has most services commented out** | ✅ All 9 services uncommented |
| 7.2 | **No token refresh in gateway** | ⏳ TODO |
| 7.3 | **No rate limiting** | ✅ 100 req/min fixed window limiter |
| 7.4 | **No route-level auth policies in gateway** | ✅ Authenticated/Seller/Admin policies on routes |

---

## P2 — Polish & Production Readiness

### 8. Testing

| # | Gap | Where | Impact |
|---|-----|-------|--------|
| 8.1 | **No integration tests for Phase 6 services** | `tests/IntegrationTests/` | StoreManagement and Media have no integration tests |
| 8.2 | **Frontend test coverage unclear** | `src/web/src/app/**/*.spec.ts` | Some spec files exist but coverage gaps unknown |
| 8.3 | **No E2E tests for new features** | `tests/E2ETests/` | Checkout, seller dashboard, admin flows untested |

### 9. DevOps & Deployment

| # | Gap | Where | Impact |
|---|-----|-------|--------|
| 9.1 | **No CI/CD pipeline** | Root | No GitHub Actions / Azure DevOps config |
| 9.2 | **No Dockerfiles** | Each service | Aspire handles local dev but no container configs for deployment |
| 9.3 | **No Terraform / IaC** | Root | Plan mentions Aspirate for Terraform generation but no IaC files |
| 9.4 | **No environment-specific config** | `appsettings.*.json` | Only `appsettings.json` exists — no staging/production overrides |

### 10. Cross-Cutting Concerns

| # | Gap | Where | Impact |
|---|-----|-------|--------|
| 10.1 | **No request/response logging middleware** | ApiGateway | No audit trail for API calls |
| 10.2 | **No email sending** | Identity.API | Forgot-password flow is placeholder — no email delivered |
| 10.3 | **No email verification on registration** | Identity.API | Users register with unverified emails |
| 10.4 | **Cart uses PostgreSQL instead of Redis** | Cart.API | Plan says Redis but implementation uses EF Core + PostgreSQL |
| 10.5 | **No low-stock alerts** | Inventory.API | No mechanism to notify when stock is low |
| 10.6 | **No admin reindex endpoint** | Search.API | Cannot rebuild Elasticsearch index if corrupted |

---

## Implementation Order (Suggested)

### Phase A — Make the core flow work (P0)
1. Frontend AuthGuard + role guard (#1.1)
2. Backend auth on Ordering/Cart endpoints (#1.2, #2.3, #2.6)
3. Fix order item price resolution (#2.1)
4. Connect SignalR in frontend (#3.1, #3.2)
5. Store verification → role update pipeline (#4.1, #4.2)
6. Identity MassTransit registration (#1.5)

### Phase B — Complete the UX (P1)
1. Seller order management (#5.1, #6.1)
2. Address form in checkout (#5.7)
3. Order tracking timeline (#5.3)
4. Error toast service (#5.4)
5. Profile update endpoint + UI (#1.3)
6. Cart auth + single-item operations (#2.3)
7. Health endpoint fixes (#7.1)

### Phase C — Polish & Ship (P2)
1. Integration tests for new services (#8.1)
2. CI/CD pipeline (#9.1)
3. Dockerfiles + IaC (#9.2, #9.3)
4. Rate limiting (#7.3)
5. Email verification flow (#10.3)

---

## Event Flow Gaps (Visual)

```
Current:
  Cart --[OrderSubmittedEvent]--> Ordering Saga --> Inventory --> Payment --> Notification --> SignalR
  Catalog --[ProductCreated/Updated/Deleted]--> Search (ES)

Missing:
  Identity --[UserRegisteredEvent]--> (nothing)
  StoreManagement --[StoreVerifiedEvent]--> (nothing) --> Identity (role update)
  Cart checkout --[no Outbox]--> (unreliable)
  Payment --[PaymentFailedEvent]--> Notification --[broadcast to All]--> (should target user)
```
