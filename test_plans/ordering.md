# Test Plan: Ordering Service

## Current Coverage

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Unit | OrderTests, OrderItemTests, AddressTests, CreateOrderHandlerTests, GetOrderByIdHandlerTests, CancelOrderHandlerTests, UpdateOrderStatusHandlerTests | ~35 | Covered |
| Integration | OrderSagaIntegrationTests | ~8 | Partially Covered |
| Contract | OrderingConsumerContractTests | ~5 | Covered |
| E2E | order-history.spec.ts | ~3 | Partially Covered |

## Test Scenarios — Unit

- [x] Order creation with valid data
- [x] OrderItem quantity must be positive
- [x] Order total calculation
- [x] Address validation
- [x] CreateOrderCommand handler
- [x] GetOrderByIdQuery handler
- [x] CancelOrderCommand handler (pending order)
- [x] UpdateOrderStatusCommand handler
- [ ] CancelOrder for shipped order (should fail)
- [ ] CancelOrder for delivered order (should fail)

## Test Scenarios — Integration

- [x] Order saga orchestration
- [ ] Saga compensation on payment failure
- [ ] Saga compensation on inventory reservation failure
- [ ] Order status transitions (Pending → Confirmed → Shipped → Delivered)
- [ ] Concurrent order creation for same inventory

## Test Scenarios — E2E

- [x] Order list display
- [x] Order detail navigation
- [x] Order empty state
- [ ] Order cancellation (DELETED — re-add)
- [ ] Saga-aware cancellation (DELETED — re-add)
- [ ] Order filtering/sorting
- [ ] Order re-order/re-purchase
- [ ] Order status timeline variations

## Gaps & Priority

| Gap | Priority | Notes |
|-----|----------|-------|
| Order cancellation E2E removed | P1 | 5 tests deleted from order-cancellation.spec.ts |
| Saga-aware cancellation removed | P1 | 3 tests deleted from saga-aware-cancellation.spec.ts |
| Saga compensation tests | P1 | Critical for data consistency |
| Order status transitions | P2 | Full lifecycle untested |
