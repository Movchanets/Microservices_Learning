# Test Plan: Checkout Feature

## Current Coverage

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Unit | — | 0 | Not Covered |
| E2E | checkout-flow.spec.ts | ~2 | Partially Covered |

## Test Scenarios — E2E

- [x] Checkout flow basic (fill address, proceed)
- [ ] Checkout edge cases (DELETED — was in checkout-edge-cases.spec.ts, 5 tests)
- [ ] Root checkout flow (DELETED — was in root/checkout-flow.spec.ts, 2 tests)
- [ ] Payment processing
- [ ] Payment failure
- [ ] Order confirmation page
- [ ] Cart merge during checkout
- [ ] Expired cart handling
- [ ] Checkout summary price accuracy

## Gaps & Priority

| Gap | Priority | Notes |
|-----|----------|-------|
| 2 E2E spec files removed | P0 | checkout-edge-cases and root/checkout-flow deleted |
| Payment processing E2E | P0 | Critical conversion path |
| Cart merge E2E | P0 | BuyerId pass-through untested |
| No unit tests | P1 | Checkout store/service has frontend unit specs but no dedicated test file |
