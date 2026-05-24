# Final State — 2026-05-17 (Updated after Plan 11 Review)

## Summary

Reviewed ordering flow audit, verified all 5 fixes applied, ran full test suite. Created comprehensive project state documentation. Implemented Plan 11 (Saga-Aware Cancellation) and conducted thorough code review.

## Plan 11 — Saga-Aware Cancellation (Implemented + Reviewed)

### What Changed
- `CancelOrderHandler` refactored from direct aggregate mutation to saga-coordinated cancellation
- `OrderStateMachine` handles `CancelOrder` event in `ReservingInventory` and `ProcessingPayment` states
- Saga compensation: publishes `CancelReservationCommand` (inventory release) + `OrderCancelledEvent`
- `CancelOrderEvent` added to SharedContracts
- `OrderCancelledEvent` has `DateTime Timestamp = default` field
- Unit tests: 5 tests covering success, not-found, completed, cancelled, faulted states
- Handler documents eventual consistency pattern (lines 21-26)
- TODO comments for `RefundPaymentCommand` on both ProcessingPayment cancel paths

### Code Review Findings
| # | Severity | Issue | Status |
|---|----------|-------|--------|
| 1 | CRITICAL | No contract test for buyer-initiated cancellation path | OPEN |
| 2 | CRITICAL | No E2E spec (saga-aware-cancellation.spec.ts) | OPEN |
| 3 | MAJOR | CancelOrderEvent missing CorrelatedBy<Guid> | OPEN |
| 4 | MAJOR | No RefundPaymentCommand (TODO in code) | TRACKED |
| 5 | MINOR | InventoryReleasedEvent dead publish (pre-existing) | DEFERRED |

### Accepted Decisions
- Race condition: Handler validation is best-effort fast-fail, saga During() is real guard
- Duplicated When(CancelOrder) blocks: MassTransit DSL doesn't support clean helper extraction
- CancelReservationCommand vs ReleaseInventoryCommand: Avoids contract proliferation
- Eventual consistency: OrderConsumerHelpers retries 5× with 200ms delay

## Ordering Flow Audit — 5 Fixes Verified

All fixes from the audit at `project_state/2026-05-17/ordering-flow-audit.md` confirmed in source code:

### Fix 1: Cart Checkout Address Forwarding
- `CartEndpoints.cs` — `CheckoutRequest` record binds address fields
- `CheckoutCartCommand` — accepts and forwards address fields
- `CheckoutCartCommandHandler` — includes address in `OrderSubmittedEvent`
- Contract test `OrderSubmitted_WithShippingAddress_ShouldPersistAllFields` validates

### Fix 2: SignalR Buyer Targeting
- `NotificationService` (frontend) — sends `?buyerId=` query string
- `BuyerIdUserIdProvider` (backend) — resolves from query string first, header fallback

### Fix 3: SignalR Lifecycle
- `AuthStore.login()/register()/checkAuth()` — starts SignalR after user fetched
- `AuthStore.logout()` — stops SignalR

### Fix 4: Order Read Model Sync
- 4 projection consumers in `Ordering.Infrastructure/Messaging/Consumers/`:
  - `OrderInventoryReservedConsumer` — Submitted → InventoryReserved
  - `OrderPaymentProcessingConsumer` — InventoryReserved → PaymentProcessing
  - `OrderCompletedProjectionConsumer` — marks Completed
  - `OrderCancelledProjectionConsumer` — marks Cancelled
- `OrderConsumerHelpers.LoadOrderAsync` — 5 retries with 200ms delay

### Fix 5: Payment Failure Persistence
- `ProcessPaymentHandler` — creates `PaymentTransaction` before checking success/failure
- Failed transactions now visible via `GET /api/payments/order/{id}`

## Test Results

### Backend (.NET) — 321 tests

| Category | Tests | Status |
|----------|-------|--------|
| Unit Tests (11 projects) | 218+ | ✅ All passing (Ordering: 68) |
| Contract Tests (9 files) | 45 | ✅ All passing |
| Integration Tests (6 projects) | 30 | ✅ All passing |
| Integration Tests (Search) | 6 | ❌ All failing (no Elasticsearch) |

### Frontend (Vitest) — 293 tests, 36 spec files

All passing. Zero failures.

### E2E (Playwright) — 18 spec files

Not re-run in this session. Pre-existing auth infrastructure issue (Playwright fill() doesn't trigger Angular reactive form change detection).

## Build Status

`dotnet build Marketplace.slnx` — 0 errors, 61 warnings (NuGet vulnerability advisories for OpenTelemetry packages).

## Changes Since 2026-05-16

| Area | Change |
|------|--------|
| Cart API | Single-item endpoints, CheckoutRequest with address binding, SellerId propagation |
| Ordering | 4 projection consumers, saga-aware cancellation (Plan 11) |
| Payment | Failed transaction persistence |
| SharedContracts | CancelOrderEvent, CancelReservationCommand |
| Frontend | SignalR lifecycle, query string buyerId, sellerId in cart |
| Tests | Contract test suite (45 tests), Ordering unit tests (68), SellerId tests, CancelOrder handler tests |

## Residual Gaps

1. **Plan 11 contract test** — buyer-initiated cancellation path untested at integration level
2. **Plan 11 E2E spec** — page objects exist, spec file missing
3. **Plan 11 CorrelatedBy<Guid>** — CancelOrderEvent missing interface (works via explicit CorrelateById)
4. **RefundPaymentCommand** — no refund infrastructure in Payment service
5. **Search.IntegrationTests** — 6 failures (Elasticsearch not running)
6. **NuGet vulnerabilities** — 61 OpenTelemetry warnings
