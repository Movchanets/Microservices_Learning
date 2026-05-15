# Ordering Service — Unit Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Ordering/`

## Goal
Implement unit tests for the Ordering domain and application layers using xUnit, Moq, and FluentAssertions.

## Scope
- **In**: `Order` aggregate, `OrderItem` entity, `Address` value object, state transitions, MediatR handlers.
- **Out**: PostgreSQL querying, MassTransit saga, EF Core persistence.

## Action Items

[ ] **Step 1: Set up Project**
  - Create `tests/UnitTests/Ordering.UnitTests` referencing `Ordering.Domain` and `Ordering.Application`.
  - Install packages: `xunit`, `Moq`, `FluentAssertions`.

[ ] **Step 2: Domain Layer Tests (`Order` Aggregate)**
  - Test: `Order.Create` initializes with `Submitted` status, correct `BuyerId`, and `CreatedAt`.
  - Test: `Order.Create` throws `DomainException` when `BuyerId` is empty or whitespace.
  - Test: `Order.AddItem` adds item with correct SKU, price, quantity.
  - Test: `Order.AddItem` replaces existing item with same SKU (upsert behavior).
  - Test: `Order.AddItem` throws `DomainException` when order is not in `Submitted` status.
  - Test: `Order.MarkInventoryReserved` transitions from `Submitted` to `InventoryReserved`.
  - Test: `Order.MarkInventoryReserved` throws when not in `Submitted` status.
  - Test: `Order.MarkPaymentProcessing` transitions from `InventoryReserved` to `PaymentProcessing`.
  - Test: `Order.MarkPaymentProcessing` throws when not in `InventoryReserved` status.
  - Test: `Order.MarkCompleted` transitions to `Completed`, sets `CompletedAt`, raises `OrderCompletedDomainEvent`.
  - Test: `Order.MarkCompleted` throws when not in `PaymentProcessing` status.
  - Test: `Order.MarkCancelled` transitions to `Cancelled`, sets `CancellationReason`, raises `OrderCancelledDomainEvent`.
  - Test: `Order.MarkCancelled` throws when in `Completed` or `Cancelled` status.
  - Test: `Order.MarkFaulted` transitions to `Faulted` from any non-terminal state.
  - Test: `Order.TotalAmount` computes sum of all item `TotalPrice`.

[ ] **Step 3: Domain Layer Tests (`OrderItem` Entity)**
  - Test: `OrderItem` constructor validates SKU not empty.
  - Test: `OrderItem` constructor validates quantity > 0.
  - Test: `OrderItem` constructor validates unitPrice >= 0.
  - Test: `OrderItem.TotalPrice` returns `UnitPrice * Quantity`.

[ ] **Step 4: Domain Layer Tests (`Address` Value Object)**
  - Test: `Address` constructor validates required fields (Street, City, Country, ZipCode).
  - Test: Two `Address` instances with same values are equal.
  - Test: Two `Address` instances with different values are not equal.

[ ] **Step 5: Application Layer Tests (`CreateOrderHandler`)**
  - Test: Handler creates order, adds items, calls repository `Add` and `SaveChangesAsync`.
  - Test: Handler returns `Result<Guid>` with the new order ID.

[ ] **Step 6: Application Layer Tests (`CancelOrderHandler`)**
  - Test: Handler loads order, calls `MarkCancelled`, returns success.
  - Test: Handler returns failure when order not found.

[ ] **Step 7: Application Layer Tests (Queries)**
  - Test: `GetOrderByIdHandler` returns `OrderDto` when order exists.
  - Test: `GetOrderByIdHandler` returns failure when order not found.
  - Test: `ListOrdersByBuyerHandler` returns list of orders for the buyer.

[ ] **Step 8: Validation**
  - Run `dotnet test tests/UnitTests/Ordering.UnitTests/Ordering.UnitTests.csproj`.
