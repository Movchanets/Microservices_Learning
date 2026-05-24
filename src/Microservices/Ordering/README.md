# Ordering Microservice

## Overview
The Ordering microservice manages the full order lifecycle through a MassTransit state machine saga. It orchestrates inventory reservation and payment processing with compensating transactions, ensuring consistency across distributed services.

## Architecture
4-layer Clean Architecture with CQRS via MediatR:

- **Ordering.Domain**: `Order` aggregate, `OrderItem` entity, `Address` value object, `OrderStatus` enum. Zero external dependencies.
- **Ordering.Application**: CQRS commands/queries (`CreateOrder`, `CancelOrder`, `GetOrderById`, `ListOrdersByBuyer`) with FluentValidation.
- **Ordering.Infrastructure**: EF Core `OrderingDbContext` (PostgreSQL) with MassTransit Outbox and Saga state persistence, `OrderRepository`.
- **Ordering.API**: `OrderStateMachine` saga, Minimal API endpoints, Aspire service wiring.

## Saga Flow
```
Cart Checkout
  └─► OrderSubmittedEvent
        └─► [Saga: ReservingInventory]
              ├─► ReserveInventoryCommand ──► Inventory Service
              │     ├─► InventoryReservedEvent ──► [Saga: ProcessingPayment]
              │     │     └─► ProcessPaymentCommand ──► Payment Service
              │     │           ├─► PaymentCompletedEvent ──► [Saga: Completed]
              │     │           └─► PaymentFailedEvent ──► CancelReservationCommand (compensation)
              │     │                                      └─► [Saga: Cancelled]
              │     └─► InventoryReservationFailedEvent ──► [Saga: Faulted]
```

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/orders/` | Create a new order |
| `GET` | `/api/orders/{id}` | Get order by ID |
| `GET` | `/api/orders/buyer/{buyerId}` | List orders by buyer |

## Quick Start

### Prerequisites
- .NET 10 SDK
- PostgreSQL + RabbitMQ (provided by Aspire AppHost)

### Build
```bash
dotnet build src/Microservices/Ordering/Ordering.API/Ordering.API.csproj
```

### Run (via AppHost recommended)
```bash
dotnet run --project src/Aspire/Marketplace.AppHost/Marketplace.AppHost.csproj
```

## Project Structure
```
Ordering/
├── Ordering.Domain/          # Aggregates, ValueObjects, Events, Enums
├── Ordering.Application/     # CQRS Commands, Queries, Validators, DTOs
├── Ordering.Infrastructure/  # EF Core, Repositories, Saga State, Outbox
└── Ordering.API/             # Saga StateMachine, Minimal API, Program.cs
```
