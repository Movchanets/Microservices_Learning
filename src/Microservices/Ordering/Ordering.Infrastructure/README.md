# Ordering.Infrastructure

## Purpose
Infrastructure layer providing EF Core persistence, repository implementations, and MassTransit saga state storage.

## Key Components

### Persistence
- **`OrderingDbContext`** — EF Core context implementing `IUnitOfWork`. Configures `Order` and `OrderItem` entities, MassTransit Outbox tables (`InboxState`, `OutboxMessage`, `OutboxState`).
- **`OrderState`** — Saga state entity implementing `SagaStateMachineInstance`. Stores `CorrelationId`, `CurrentState`, `BuyerId`, `OrderId`, `TotalAmount`, `ItemsJson` (serialized), `RowVersion`.
- **`OrderConfiguration`** — EF Core config for `Order` with `Address` as owned type, `Items` as owned collection, index on `BuyerId`.
- **`OrderItemConfiguration`** — EF Core config with `decimal(18,2)` for price, ignores computed `TotalPrice`.

### Repositories
- **`OrderRepository`** — Implements `IOrderRepository` with eager loading (`Include(o => o.Items)`).

### DependencyInjection
- `AddOrderingInfrastructure()` — Registers `IOrderRepository`, `IUnitOfWork`

## Database
- **Provider**: PostgreSQL via Npgsql
- **Schema**: Auto-generated from EF Core configurations
- **Outbox**: MassTransit EF Core Outbox for guaranteed message delivery

## Dependencies
- `Ordering.Application` — Domain types, repository interface
- `MassTransit.EntityFrameworkCore` 8.3.5, `MassTransit.RabbitMQ` 8.3.5
- `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.1
