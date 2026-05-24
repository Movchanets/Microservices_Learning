# Test Plan: Notification Service

## Current Coverage

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Unit | OrderCompletedConsumerTests, OrderCancelledConsumerTests, OrderStatusChangedConsumerTests, UserIdProviderTests | ~15 | Covered |
| Integration | — | 0 | Not Covered |
| Contract | NotificationContractTests | ~5 | Covered |
| E2E | — | 0 | Not Covered |

## Test Scenarios — Unit

- [x] OrderCompletedConsumer sends notification
- [x] OrderCancelledConsumer sends notification
- [x] OrderStatusChangedConsumer sends notification
- [x] UserIdProvider returns correct user ID
- [ ] Consumer handles missing user gracefully
- [ ] Consumer handles SignalR hub failure
- [ ] Notification deduplication

## Test Scenarios — Integration

- [ ] SignalR hub connection and message delivery
- [ ] Redis backplane cross-instance delivery
- [ ] Consumer → Hub message flow end-to-end

## Test Scenarios — E2E

- [ ] Real-time order status notification
- [ ] Notification toast display
- [ ] SignalR reconnection after disconnect

## Gaps & Priority

| Gap | Priority | Notes |
|-----|----------|-------|
| SignalR integration test | P2 | Hub + backplane untested |
| Notification E2E | P2 | Low priority — real-time is nice-to-have |
