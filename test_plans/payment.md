# Test Plan: Payment Service

## Current Coverage

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Unit | PaymentTransactionTests, RefundTests, ProcessPaymentHandlerTests, RefundPaymentHandlerTests, MockPaymentGatewayTests | ~25 | Covered |
| Integration | — | 0 | Not Covered |
| Contract | PaymentContractTests | ~5 | Covered |
| E2E | — | 0 | Not Covered |

## Test Scenarios — Unit

- [x] PaymentTransaction creation
- [x] PaymentTransaction status transitions
- [x] Refund creation and validation
- [x] ProcessPaymentCommand handler
- [x] RefundPaymentCommand handler
- [x] MockPaymentGateway success/failure paths
- [ ] Payment idempotency (duplicate request)
- [ ] Payment amount validation (negative, zero)

## Test Scenarios — Integration

- [ ] Payment processing with database persistence
- [ ] Payment event publication (PaymentProcessedEvent)
- [ ] Refund event publication (PaymentRefundedEvent)
- [ ] Payment failure retry behavior

## Test Scenarios — E2E

- [ ] Payment during checkout (success)
- [ ] Payment failure during checkout
- [ ] Admin refund flow (DELETED — was in payment-refund.spec.ts)
- [ ] Payment status display in order detail

## Gaps & Priority

| Gap | Priority | Notes |
|-----|----------|-------|
| Integration tests | P1 | No integration test project for Payment |
| Payment E2E | P1 | Checkout flow covers happy path but not failure |
| Refund E2E removed | P2 | 5 tests deleted from payment-refund.spec.ts |
