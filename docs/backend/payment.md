# Payment Service

> **Last Updated:** 2026-06-20

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Full 4-layer (Domain → Application → Infrastructure → API) |
| **Database** | PostgreSQL (EF Core + Npgsql) |
| **Messaging** | RabbitMQ via MassTransit (with EF Outbox) |
| **Concurrency** | Optimistic (retry on `DbUpdateConcurrencyException` for refunds) |
| **Project Path** | `src/Microservices/Payment/` |

## Key Domain Entities

| Entity | Type | Key Properties |
|:---|:---|:---|
| `PaymentTransaction` | Aggregate Root | OrderId (Guid), BuyerId (string), Amount, Status, TransactionId (gateway ref), FailureReason, CreatedAt, ProcessedAt |
| `Refund` | Child Entity | TransactionId, OrderId, Amount, Reason, Status, GatewayRefundId, CreatedAt, ProcessedAt |

### Payment Status Flow

```
Pending → Completed → Refunded
   │
   └──► Failed
```

| Value | Int | Description |
|:---|:---:|:---|
| `Pending` | 0 | Payment initiated, awaiting gateway response |
| `Completed` | 1 | Payment successful, `TransactionId` set from gateway |
| `Failed` | 2 | Payment failed, `FailureReason` set |
| `Refunded` | 3 | Completed payment was refunded |

### Refund Status Flow

```
Pending → Processed
   │
   └──► Failed
```

| Value | Int | Description |
|:---|:---:|:---|
| `Pending` | 0 | Refund initiated |
| `Processed` | 1 | Refund successful, `GatewayRefundId` set |
| `Failed` | 2 | Refund failed |

## API Endpoints (`/api/payments`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/order/{orderId:guid}` | Direct repo lookup | Authenticated (buyer or Admin) |
| `POST` | `/{transactionId:guid}/refund` | `RefundPaymentCommand` | Admin (body: `{ reason, amount? }`) |

**Payment retrieval** has ownership check: only the buyer who made the payment or an Admin can view it. Returns transaction details plus associated refunds.

**Refund endpoint** has built-in retry logic (max 2 retries on `DbUpdateConcurrencyException`).

## Integration Events

### Consumed

| Event/Command | Consumer | Action |
|:---|:---|:---|
| `ProcessPaymentCommand` | `ProcessPaymentConsumer` | Creates PaymentTransaction, calls gateway, publishes success/failure event |
| `RefundPaymentIntegrationCommand` | `RefundPaymentConsumer` | Looks up transaction by OrderId, creates refund via `RefundPaymentCommand`, publishes `PaymentRefundedEvent` |

### Published (via Outbox)

| Event | Trigger |
|:---|:---|
| `PaymentCompletedEvent` | Payment gateway returns success |
| `PaymentFailedEvent` | Payment gateway returns failure |
| `PaymentRefundedEvent` | Refund processed successfully (`Refund.MarkProcessed()`) |

## Current Status & Known Issues

- ✅ Full refund flow with retry on concurrency conflicts
- ✅ Ownership check on payment retrieval (buyer or Admin)
- ✅ Refund status tracking with gateway reference
- ✅ Compensation consumer (`RefundPaymentConsumer`) for saga-driven refunds
- ⚠️ Payment gateway is simulated (no real Stripe/PayPal integration)
