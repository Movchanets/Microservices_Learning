# Payment.API

## Purpose
ASP.NET Core Minimal API hosting payment endpoints and the `ProcessPaymentConsumer` MassTransit consumer.

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/payments/order/{orderId}` | Get payment transaction status for an order |

## Program.cs Wiring
1. `AddServiceDefaults()` — Aspire telemetry, health, resilience
2. `AddNpgsqlDbContext<PaymentDbContext>("payment-db")` — Database
3. `AddPaymentInfrastructure()` — Repository, UoW, gateway
4. `AddMediatR()` with `ValidationBehavior` + `LoggingBehavior`
5. `AddMassTransit()` — Consumer + Outbox + RabbitMQ

## Dependencies
- `Payment.Infrastructure` — Persistence, consumers, DI extensions
- `Marketplace.ServiceDefaults` — Aspire shared config
- `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` 13.3.2
- `Aspire.RabbitMQ.Client` 13.3.2
