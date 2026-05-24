# Ordering Service — Integration Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Ordering/`

## Goal
Implement integration tests for the Ordering service testing PostgreSQL repository, EF Core saga state persistence, MassTransit consumers, and the OrderStateMachine saga using Testcontainers.

## Scope
- **In**: `OrderRepository`, `OrderingDbContext`, `OrderStateMachine` saga, `OrderSubmittedConsumer`.
- **Out**: UI testing, external microservices (Inventory, Payment).

## Action Items

[ ] **Step 1: Set up Project**
  - Create `tests/IntegrationTests/Ordering.IntegrationTests` referencing `Ordering.Infrastructure` and `Ordering.API`.
  - Install packages: `xunit`, `FluentAssertions`, `Testcontainers.PostgreSql`, `MassTransit.TestFramework`.

[ ] **Step 2: Test Fixture Setup**
  - Create `OrderingDatabaseFixture` utilizing `PostgreSqlContainer`.
  - Apply `OrderingDbContext` schema on startup (including Outbox and Saga state tables).

[ ] **Step 3: Repository Tests**
  - Test: Save and retrieve an `Order` with items — verify items are eagerly loaded.
  - Test: `GetByBuyerIdAsync` returns orders for the specified buyer only.
  - Test: `GetByBuyerIdAsync` returns empty list for buyer with no orders.

[ ] **Step 4: OrderStateMachine Saga Tests**
  - Test: `OrderSubmitted` event → saga transitions to `ReservingInventory`, publishes `ReserveInventoryCommand`.
  - Test: `InventoryReserved` event → saga transitions to `ProcessingPayment`, publishes `ProcessPaymentCommand`.
  - Test: `InventoryFailed` event → saga transitions to `Faulted`, publishes `OrderCancelledEvent`.
  - Test: `PaymentCompleted` event → saga transitions to `Completed`, publishes `OrderCompletedEvent`.
  - Test: `PaymentFailed` event → saga transitions to `Cancelled`, publishes `CancelReservationCommand` + `OrderCancelledEvent` (compensation).
  - Test: Idempotency — duplicate events do not create duplicate state transitions.

[ ] **Step 5: Consumer Integration Tests**
  - Test: `OrderSubmittedConsumer` creates `Order` entity in database from `OrderSubmittedEvent`.
  - Test: `OrderSubmittedConsumer` is idempotent — duplicate event does not create duplicate order.

[ ] **Step 6: Validation**
  - Run `dotnet test tests/IntegrationTests/Ordering.IntegrationTests/Ordering.IntegrationTests.csproj`.
