# Notification Service — Unit Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Notification/`

## Goal
Implement unit tests for the Notification.Worker consumers and hub using xUnit, Moq, and FluentAssertions.

## Scope
- **In**: MassTransit consumers, `OrderUpdateMessage`, `BuyerIdUserIdProvider`.
- **Out**: SignalR WebSocket transport, Redis backplane.

## Action Items

[ ] **Step 1: Set up Project**
  - Create `tests/UnitTests/Notification.UnitTests` referencing `Notification.Worker`.
  - Install packages: `xunit`, `Moq`, `FluentAssertions`, `MassTransit.TestFramework`.

[ ] **Step 2: Consumer Tests (`OrderCompletedConsumer`)**
  - Test: Consumer sends `OrderUpdate` to correct buyer via `IHubContext.Clients.User(buyerId)`.
  - Test: Message has `Status = "Completed"` and correct `OrderId`.
  - Test: Message has `Reason = null`.

[ ] **Step 3: Consumer Tests (`OrderCancelledConsumer`)**
  - Test: Consumer sends `OrderUpdate` to correct buyer.
  - Test: Message has `Status = "Cancelled"` and carries the `Reason`.

[ ] **Step 4: Consumer Tests (`PaymentFailedConsumer`)**
  - Test: Consumer sends `OrderUpdate` to all clients (broadcast).
  - Test: Message has `Status = "PaymentFailed"` and carries `FailureReason`.

[ ] **Step 5: Consumer Tests (`InventoryReservationFailedConsumer`)**
  - Test: Consumer sends `OrderUpdate` to all clients (broadcast).
  - Test: Message has `Status = "InventoryFailed"` and carries `Reason`.

[ ] **Step 6: UserIdProvider Tests**
  - Test: `BuyerIdUserIdProvider.GetUserId` returns value from `x-buyer-id` header.
  - Test: `BuyerIdUserIdProvider.GetUserId` returns null when header is missing.

[ ] **Step 7: Validation**
  - Run `dotnet test tests/UnitTests/Notification.UnitTests/Notification.UnitTests.csproj`.
