# Ordering.Domain

## Purpose
Pure domain layer for the Ordering bounded context. Contains business rules, aggregates, value objects, and domain events. Zero external dependencies.

## Key Types

### Aggregates
- **`Order`** — Aggregate root managing the order lifecycle. Factory method `Create(buyerId)`. Enforces state machine transitions via `MarkInventoryReserved()`, `MarkPaymentProcessing()`, `MarkCompleted()`, `MarkCancelled(reason)`, `MarkFaulted(reason)`.
- **`OrderItem`** — Entity within the order. Created via internal constructor with `Sku`, `ProductName`, `UnitPrice`, `Quantity`. Computed `TotalPrice`.

### Value Objects
- **`Address`** — Immutable value object with `Street`, `City`, `State`, `Country`, `ZipCode`. Extends `ValueObject` for structural equality.

### Enums
- **`OrderStatus`** — `Submitted(0)`, `InventoryReserved(1)`, `PaymentProcessing(2)`, `Completed(3)`, `Cancelled(4)`, `Faulted(5)`

### Domain Events
- **`OrderCompletedDomainEvent`** — Raised when order reaches `Completed` status
- **`OrderCancelledDomainEvent`** — Raised when order is cancelled

### Exceptions
- **`DomainException`** — Thrown on invalid state transitions or business rule violations

### Interfaces
- **`IOrderRepository`** — Extends `IRepository<Order>`, adds `GetByBuyerIdAsync(buyerId)`

## Dependencies
- `BuildingBlocks.SharedContracts` — `AggregateRoot`, `Entity`, `ValueObject`, `IDomainEvent`, `IRepository<T>`
