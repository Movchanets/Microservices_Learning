# Test Plan: Inventory Service

## Current Coverage

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Unit | InventoryItemTests, ReserveStockCommandHandlerTests, ReleaseStockCommandHandlerTests | ~18 | Covered |
| Integration | InventoryItemRepositoryTests, ReservationConsumerTests | ~10 | Partially Covered |
| Contract | InventoryReservationContractTests | ~5 | Covered |
| E2E | — | 0 | Not Covered |

## Test Scenarios — Unit

- [x] InventoryItem creation
- [x] Stock reservation reduces available
- [x] Stock release restores available
- [x] ReserveStockCommand handler
- [x] ReleaseStockCommand handler
- [ ] ReserveStock with insufficient quantity
- [ ] Negative stock prevention
- [ ] Concurrent reservation race condition

## Test Scenarios — Integration

- [x] InventoryItemRepository CRUD
- [x] ReservationConsumer handles ReserveStockCommand
- [ ] ReleaseConsumer handles ReleaseStockCommand
- [ ] Inventory updated after order completion
- [ ] Concurrent reservation with optimistic concurrency

## Test Scenarios — E2E

- [ ] Seller inventory management page (DELETED — was in inventory-management.spec.ts)
- [ ] Stock level display on product detail
- [ ] Out-of-stock indicator on catalog
- [ ] Low stock alert threshold

## Gaps & Priority

| Gap | Priority | Notes |
|-----|----------|-------|
| Inventory management E2E removed | P1 | 4 tests deleted from inventory-management.spec.ts |
| Insufficient stock unit test | P1 | Edge case for negative/zero stock |
| Concurrent reservation | P2 | Optimistic concurrency behavior |
