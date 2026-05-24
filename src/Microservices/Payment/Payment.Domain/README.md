# Payment.Domain

## Purpose
Pure domain layer for the Payment bounded context. Contains the payment transaction aggregate and status enumeration. Zero external dependencies.

## Key Types

### Aggregates
- **`PaymentTransaction`** — Aggregate root representing a payment attempt. Factory method `Create(orderId, buyerId, amount)`. Methods: `MarkCompleted(transactionId)`, `MarkFailed(reason)`.
  - Properties: `OrderId`, `BuyerId`, `Amount`, `Status`, `TransactionId`, `FailureReason`, `CreatedAt`, `ProcessedAt`

### Enums
- **`PaymentStatus`** — `Pending(0)`, `Completed(1)`, `Failed(2)`, `Refunded(3)`

### Interfaces
- **`IPaymentTransactionRepository`** — Extends `IRepository<PaymentTransaction>`, adds `GetByOrderIdAsync(orderId)`

## Dependencies
- `BuildingBlocks.SharedContracts` — `AggregateRoot`, `IRepository<T>`
