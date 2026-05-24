# Payment.Infrastructure

## Purpose
Infrastructure layer providing EF Core persistence, MassTransit consumer, payment gateway abstraction, and Outbox configuration.

## Key Components

### Messaging
- **`ProcessPaymentConsumer`** — MassTransit `IConsumer<ProcessPaymentCommand>`. Calls `IPaymentGateway`, persists transaction via MediatR, publishes `PaymentCompletedEvent` or `PaymentFailedEvent`.

### External
- **`IPaymentGateway`** — Interface for external payment processing. Method: `ProcessPaymentAsync(orderId, amount, buyerId)` returns `PaymentGatewayResult(IsSuccess, TransactionId, FailureReason)`.
- **`MockPaymentGateway`** — Development implementation. Always succeeds with `txn_{guid}`.

### Persistence
- **`PaymentDbContext`** — EF Core context implementing `IUnitOfWork`. Configures `PaymentTransaction` entity and MassTransit Outbox tables.
- **`PaymentTransactionConfiguration`** — EF Core config with `decimal(18,2)` for amount, index on `OrderId`.
- **`PaymentTransactionRepository`** — Implements `IPaymentTransactionRepository`.

### DependencyInjection
- `AddPaymentInfrastructure()` — Registers repository, UoW, `MockPaymentGateway` as singleton

## Database
- **Provider**: PostgreSQL via Npgsql
- **Outbox**: MassTransit EF Core Outbox for guaranteed message delivery

## Dependencies
- `Payment.Application` — Commands, domain types
- `MassTransit.EntityFrameworkCore` 8.3.5, `MassTransit.RabbitMQ` 8.3.5
- `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.1
