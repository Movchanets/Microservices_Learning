# Payment Service

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Full 4-layer (Domain → Application → Infrastructure → API) |
| **Database** | PostgreSQL (EF Core + Npgsql) |
| **Messaging** | RabbitMQ via MassTransit (with EF Outbox) |
| **Concurrency** | Optimistic (retry on DbUpdateConcurrencyException for refunds) |
| **Project Path** | `src/Microservices/Payment/` |

## Key Domain Entities

| Entity | Type | Key Properties |
|:---|:---|:---|
| `PaymentTransaction` | Aggregate Root | OrderId, BuyerId, Amount, Status, TransactionId (gateway ref), FailureReason |
| `Refund` | Entity | TransactionId, OrderId, Amount, Reason, Status, GatewayRefundId |

### Payment Status Flow

```
Pending → Completed
   │
   └──► Failed

Completed → Refunded
```

### Refund Status Flow

```
Pending → Processed
   │
   └──► Failed
```

## API Endpoints (`/api/payments`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/order/{orderId}` | Direct lookup | Authenticated (buyer or Admin) |
| `POST` | `/{transactionId}/refund` | `RefundPaymentCommand` | Admin |

**Refund endpoint** has built-in retry logic (max 2 retries on `DbUpdateConcurrencyException`).

## Integration Events

### Consumed

| Event | Consumer | Action |
|:---|:---|:---|
| `PaymentRequestedEvent` | (consumer) | Creates PaymentTransaction, simulates gateway call |

### Published (via Outbox)

| Event | Trigger |
|:---|:---|
| `PaymentCompletedEvent` | PaymentTransaction.MarkCompleted() |
| `PaymentFailedEvent` | PaymentTransaction.MarkFailed() |
| `PaymentRefundedEvent` | Refund.MarkProcessed() |

## Current Status & Known Issues

- ✅ Full refund flow with retry on concurrency conflicts
- ✅ Ownership check on payment retrieval (buyer or Admin)
- ✅ Refund status tracking with gateway reference
- ⚠️ Payment gateway is simulated (no real Stripe/PayPal integration)
