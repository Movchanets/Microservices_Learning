# Marketplace — Missing Capabilities

**Purpose**: Gap analysis for a basic functional marketplace. Items are grouped by priority (P0 = critical for MVP, P1 = important, P2 = nice-to-have).

**Last updated**: 2026-05-15
**P0 Fix Plans**: `implementation_plan/p0-fixes/` (6 sub-plans)
**P1 Fix Plans**: `implementation_plan/p1-fixes/` (6 sub-plans)
**P2 Fix Plans**: `implementation_plan/p2-fixes/` (5 sub-plans)

---

## P0 — Critical for MVP (blocks basic buyer/seller flow)

### 1. Authentication & Authorization Gaps

| # | Gap | Where | Impact |
|---|-----|-------|--------|
| 1.1 | **No frontend AuthGuard** | `src/web/src/app/core/auth/` | All routes accessible without login. Need `auth.guard.ts` + `role.guard.ts` |
| 1.2 | **No role-based authorization on backend** | Ordering, Cart, Inventory, Payment endpoints | Any user can access any endpoint. Need `[Authorize(Roles = "Seller")]` or policy-based auth |
| 1.3 | **No profile update endpoint** | Identity.API | Users cannot edit name/email after registration |
| 1.4 | **No admin role enforcement on store verification** | `StoreManagement.API/Endpoints/StoreEndpoints.cs` | `.RequireAuthorization()` exists but no `RequireAuthorization("Admin")` policy |
| 1.5 | **Identity has no MassTransit registration** | `Identity.API/Program.cs` | `UserRegisteredEvent` / `UserRoleChangedEvent` domain events are dead — never published to bus |

### 2. Order Flow Broken

| # | Gap | Where | Impact |
|---|-----|-------|--------|
| 2.1 | **Order items have price hardcoded to 0** | `Ordering.Infrastructure/Consumers/OrderSubmittedConsumer.cs:38` | `order.AddItem(item.Sku, item.Sku, 0m, item.Quantity)` — all orders have zero-value items |
| 2.2 | **No address collection in checkout** | Frontend checkout page | Order domain has `Address` value object but no UI collects it |
| 2.3 | **Cart has no auth** | `Cart.API/Endpoints/` | Relies on spoofable `x-buyer-id` header. Need JWT Bearer + extract buyer ID from token |
| 2.4 | **Cart checkout has no Outbox** | `Cart.API/Program.cs` | `OrderSubmittedEvent` published via `IPublishEndpoint` directly — no guaranteed delivery |
| 2.5 | **No order cancellation endpoint** | `Ordering.API` | Users cannot cancel orders after submission |
| 2.6 | **Ordering endpoints have no auth** | `Ordering.API/Endpoints/` | Anyone can create/view orders for any buyer |

### 3. SignalR Not Connected

| # | Gap | Where | Impact |
|---|-----|-------|--------|
| 3.1 | **Frontend NotificationService never started** | `src/web/src/app/core/signalr/` | `notificationService.start()` is never called — real-time updates don't reach the UI |
| 3.2 | **NotificationBridgeComponent not verified** | `src/web/src/app/core/signalr/notification-bridge.component.ts` | May not be bootstrapped in app |
| 3.3 | **Failed events broadcast to all users** | `Notification.Worker/Consumers/` | `PaymentFailedEvent` and `InventoryReservationFailedEvent` lack `BuyerId` — broadcast to `Clients.All` instead of targeted user |

### 4. Seller Verification → Role Update Missing

| # | Gap | Where | Impact |
|---|-----|-------|--------|
| 4.1 | **StoreVerifiedEvent not published as integration event** | `StoreManagement.Infrastructure/` | Domain event exists but no MassTransit publish handler |
| 4.2 | **No consumer to update user role on store verification** | Identity.API (missing) | When admin verifies a store, the seller's role stays as "Buyer" |
| 4.3 | **Seller dashboard has no guard** | `src/web/src/app/features/seller-dashboard/` | Any logged-in user can access seller pages |

---

## P1 — Important for Marketplace UX

### 5. Missing Frontend Pages/Features

| # | Gap | Where | Impact |
|---|-----|-------|--------|
| 5.1 | **No seller order management page** | `src/web/src/app/features/seller-dashboard/` | Sellers cannot view/manage orders containing their products |
| 5.2 | **No inventory management UI** | No feature exists | Sellers/admins cannot view or manage stock levels |
| 5.3 | **No order tracking/timeline** | `src/web/src/app/features/orders/` | No visual representation of saga progression (Reserved → Paid → Completed) |
| 5.4 | **No global error toast/notification service** | `src/web/src/app/shared/` | HTTP errors fail silently — users see no feedback |
| 5.5 | **No 404 page** | `src/web/src/app/` | Bad routes show blank or Angular error |
| 5.6 | **No admin role management UI** | `src/web/src/app/features/admin/` | Cannot change user roles from Buyer to Seller etc. |
| 5.7 | **No address form in checkout** | `src/web/src/app/features/checkout/` | Shipping address not collected |

### 6. Backend Endpoint Gaps

| # | Gap | Where | Impact |
|---|-----|-------|--------|
| 6.1 | **No seller order list endpoint** | Ordering.API | Sellers need `GET /api/orders/seller/{sellerId}` to see orders with their products |
| 6.2 | **No category update/delete endpoints** | Catalog.API | Only create + list exists |
| 6.3 | **No inventory list endpoint** | Inventory.API | `GET /api/inventory/items` missing — cannot browse stock |
| 6.4 | **No payment refund endpoint** | Payment.API | Cannot process refunds |
| 6.5 | **No media listing endpoint** | Media.API | Cannot browse uploaded files |
| 6.6 | **No change-password endpoint** | Identity.API | Users cannot change password while logged in |
| 6.7 | **Inventory endpoints have no auth** | Inventory.API | Anyone can add/modify stock |

### 7. Gateway & Health

| # | Gap | Where | Impact |
|---|-----|-------|--------|
| 7.1 | **Health endpoint has most services commented out** | `ApiGateway/Endpoints/HealthEndpoints.cs` | Only identity-api is probed — other services not monitored |
| 7.2 | **No token refresh in gateway** | `ApiGateway/Middleware/` | When JWT expires mid-session, no automatic refresh |
| 7.3 | **No rate limiting** | ApiGateway | No protection against abuse |
| 7.4 | **No route-level auth policies in gateway** | `appsettings.json` YARP config | All proxy routes pass through without role checks at gateway level |

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
