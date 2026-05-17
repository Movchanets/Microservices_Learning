# Final State — 2026-05-17

## Summary

Reviewed ordering flow audit, verified all 5 fixes applied, ran full test suite. Created comprehensive project state documentation.

## Ordering Flow Audit — 5 Fixes Verified

All fixes from the audit at `project_state/2026-05-17/ordering-flow-audit.md` confirmed in source code:

### Fix 1: Cart Checkout Address Forwarding
- `CartEndpoints.cs` — `CheckoutRequest` record binds address fields (AddressLine1, AddressLine2, City, State, PostalCode, Country)
- `CheckoutCartCommand` — accepts and forwards address fields
- `CheckoutCartCommandHandler` — includes address in `OrderSubmittedEvent`
- Contract test `OrderSubmitted_WithShippingAddress_ShouldPersistAllFields` validates

### Fix 2: SignalR Buyer Targeting
- `NotificationService` (frontend) — sends `?buyerId=` query string instead of custom header
- `BuyerIdUserIdProvider` (backend) — resolves from query string first, header fallback
- `NotificationHub.OnConnectedAsync` — logs buyer identity from query string

### Fix 3: SignalR Lifecycle
- `AuthStore.login()` — starts SignalR after user fetched
- `AuthStore.register()` — starts SignalR after user fetched
- `AuthStore.checkAuth()` — starts SignalR if session valid
- `AuthStore.logout()` — stops SignalR
- `app.config.ts` — no direct SignalR start in initializer

### Fix 4: Order Read Model Sync
- 4 projection consumers added to `Ordering.Infrastructure/Messaging/Consumers/`:
  - `OrderInventoryReservedConsumer` — Submitted → InventoryReserved
  - `OrderPaymentProcessingConsumer` — InventoryReserved → PaymentProcessing
  - `OrderCompletedProjectionConsumer` — marks Completed
  - `OrderCancelledProjectionConsumer` — marks Cancelled
- All publish `OrderStatusChangedEvent` for downstream consumers
- All guard against idempotent re-processing

### Fix 5: Payment Failure Persistence
- `ProcessPaymentHandler` — creates `PaymentTransaction` before checking success/failure
- Both `MarkCompleted()` and `MarkFailed()` called as appropriate
- Failed transactions now visible via `GET /api/payments/order/{id}`

## Test Results

### Backend (.NET) — 299 tests

| Category | Tests | Status |
|----------|-------|--------|
| Unit Tests (11 projects) | 218 | ✅ All passing |
| Contract Tests (9 files) | 45 | ✅ All passing |
| Integration Tests (6 projects) | 30 | ✅ All passing |
| Integration Tests (Search) | 6 | ❌ All failing (no Elasticsearch) |

### Frontend (Vitest) — 293 tests, 36 spec files

All passing. Zero failures.

### E2E (Playwright) — 18 spec files

Not re-run in this session. Pre-existing auth infrastructure issue (Playwright fill() doesn't trigger Angular reactive form change detection) affects tests needing registration.

## Build Status

`dotnet build Marketplace.slnx` — 0 errors, 122 warnings (NuGet vulnerability advisories for OpenTelemetry packages).

## Changes Since 2026-05-16

| Area | Change |
|------|--------|
| Cart API | Single-item endpoints added (POST /items, PUT /items/{sku}, DELETE /items/{sku}) |
| Cart API | CheckoutRequest with address binding |
| Ordering | 4 projection consumers for order read model sync |
| Payment | Failed transaction persistence |
| Frontend | SignalR lifecycle in AuthStore |
| Frontend | Query string buyerId transport |
| Tests | Contract test suite (45 tests, 9 files) |
| Tests | Ordering integration tests (3 saga tests) |
| Tests | E2E checkout-flow.spec.ts added |

## Files Changed (audit fixes)

```
src/Microservices/Cart/Cart.API/Endpoints/CartEndpoints.cs              # CheckoutRequest body binding + single-item endpoints
src/Microservices/Cart/Cart.Application/Commands/CheckoutCartCommand.cs  # Address fields forwarding
src/Microservices/Cart/Cart.Application/Commands/AddCartItemCommand.cs   # NEW
src/Microservices/Cart/Cart.Application/Commands/UpdateCartItemCommand.cs # NEW
src/Microservices/Cart/Cart.Application/Commands/RemoveCartItemCommand.cs # NEW
src/Microservices/Cart/Cart.Application/Commands/AddCartItemValidator.cs  # NEW
src/Microservices/Cart/Cart.Application/Commands/RemoveCartItemValidator.cs # NEW
src/Microservices/Ordering/Ordering.Infrastructure/Messaging/Consumers/OrderInventoryReservedConsumer.cs   # NEW
src/Microservices/Ordering/Ordering.Infrastructure/Messaging/Consumers/OrderPaymentProcessingConsumer.cs   # NEW
src/Microservices/Ordering/Ordering.Infrastructure/Messaging/Consumers/OrderCompletedProjectionConsumer.cs  # NEW
src/Microservices/Ordering/Ordering.Infrastructure/Messaging/Consumers/OrderCancelledProjectionConsumer.cs  # NEW
src/Microservices/Ordering/Ordering.Infrastructure/Messaging/Consumers/OrderConsumerHelpers.cs              # NEW
src/Microservices/Payment/Payment.Application/Commands/ProcessPayment/ProcessPaymentHandler.cs  # Failed txn persistence
src/Microservices/Notification/Notification.Worker/Hubs/UserIdProvider.cs      # Query string buyerId
src/Microservices/Notification/Notification.Worker/Hubs/NotificationHub.cs     # Query string logging
src/web/src/app/core/signalr/notification.service.ts                           # Query string transport
src/web/src/app/core/auth/auth.store.ts                                        # SignalR lifecycle
src/web/src/app/app.config.ts                                                  # Removed direct SignalR start
tests/ContractTests/                                                            # NEW — 45 tests across 9 files
tests/IntegrationTests/Ordering.IntegrationTests/Saga/OrderSagaIntegrationTests.cs  # NEW — 3 saga tests
tests/E2ETests/tests/checkout-flow.spec.ts                                     # NEW
```

## Residual Gaps

1. **Seller order correlation** — `OrderItem.SellerId` not reliably propagated
2. **Saga-aware cancellation** — `CancelOrderHandler` doesn't coordinate with saga compensation
3. **Search.IntegrationTests** — 6 failures (Elasticsearch not running)
4. **NuGet vulnerabilities** — 122 OpenTelemetry warnings
5. **BuildingBlocks.SharedContracts.UnitTests** — project file not found
