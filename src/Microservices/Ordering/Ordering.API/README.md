# Ordering.API

## Purpose
ASP.NET Core Minimal API hosting the `OrderStateMachine` saga, order management endpoints, and Aspire service integrations.

## Saga: OrderStateMachine

The saga orchestrates the full order lifecycle across Ordering, Inventory, and Payment services with compensating transactions.

### States
| State | Description |
|-------|-------------|
| `Initial` | Saga created, waiting for `OrderSubmittedEvent` |
| `ReservingInventory` | `ReserveInventoryCommand` sent, awaiting response |
| `ProcessingPayment` | Inventory reserved, `ProcessPaymentCommand` sent |
| `Completed` | Payment succeeded, `OrderCompletedEvent` published |
| `Cancelled` | Payment failed, inventory released via compensation |
| `Faulted` | Inventory reservation failed, order cannot proceed |

### Compensation Path
When payment fails during `ProcessingPayment`:
1. Publishes `CancelReservationCommand` to release inventory
2. Publishes `OrderCancelledEvent` for downstream services
3. Transitions to `Cancelled` state

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/orders/` | Create a new order |
| `GET` | `/api/orders/{id}` | Get order by ID |
| `GET` | `/api/orders/buyer/{buyerId}` | List orders by buyer |

## Program.cs Wiring
1. `AddServiceDefaults()` — Aspire telemetry, health, resilience
2. `AddNpgsqlDbContext<OrderingDbContext>("ordering-db")` — Database
3. `AddOrderingInfrastructure()` — Repository, UoW
4. `AddMediatR()` with `ValidationBehavior` + `LoggingBehavior`
5. `AddValidatorsFromAssemblyContaining<CreateOrderValidator>()`
6. `AddMassTransit()` — Saga + EF Core repository + Outbox + RabbitMQ

## Dependencies
- `Ordering.Infrastructure` — Persistence, DI extensions
- `Marketplace.ServiceDefaults` — Aspire shared config
- `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` 13.3.2
- `Aspire.RabbitMQ.Client` 13.3.2
