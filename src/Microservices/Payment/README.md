# Payment Microservice

## Overview
The Payment microservice processes payments for orders. It consumes `ProcessPaymentCommand` from the Ordering saga, calls an external payment gateway (mock for development), and publishes result events back to the saga.

## Architecture
4-layer Clean Architecture with CQRS via MediatR:

- **Payment.Domain**: `PaymentTransaction` aggregate, `PaymentStatus` enum. Zero external dependencies.
- **Payment.Application**: `ProcessPaymentInternalCommand` handler for transaction persistence.
- **Payment.Infrastructure**: EF Core `PaymentDbContext`, `ProcessPaymentConsumer`, `MockPaymentGateway`, `IPaymentGateway` interface.
- **Payment.API**: Payment status query endpoint, MassTransit consumer registration.

## Message Flow
```
Ordering Saga
  └─► ProcessPaymentCommand
        └─► [ProcessPaymentConsumer]
              ├─► IPaymentGateway.ProcessPaymentAsync()
              ├─► ProcessPaymentInternalCommand (persist transaction)
              ├─► PaymentCompletedEvent ──► Ordering Saga (success)
              └─► PaymentFailedEvent ──► Ordering Saga (failure)
```

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/payments/order/{orderId}` | Get payment status for an order |

## Quick Start

### Prerequisites
- .NET 10 SDK
- PostgreSQL + RabbitMQ (provided by Aspire AppHost)

### Build
```bash
dotnet build src/Microservices/Payment/Payment.API/Payment.API.csproj
```

### Run (via AppHost recommended)
```bash
dotnet run --project src/Aspire/Marketplace.AppHost/Marketplace.AppHost.csproj
```

## Project Structure
```
Payment/
├── Payment.Domain/          # Aggregates, Enums
├── Payment.Application/     # CQRS Commands
├── Payment.Infrastructure/  # EF Core, Consumers, Gateway, Outbox
└── Payment.API/             # Endpoints, Program.cs
```

## Payment Gateway
The service uses `IPaymentGateway` abstraction for external gateway integration:
- **Development**: `MockPaymentGateway` always succeeds, returns `txn_{guid}` as transaction ID
- **Production**: Implement `IPaymentGateway` with Stripe/PayPal SDK
