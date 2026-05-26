# Ordering Service

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Full 4-layer (Domain → Application → Infrastructure → API) |
| **Database** | PostgreSQL (EF Core + Npgsql) |
| **Messaging** | RabbitMQ via MassTransit (with EF Outbox) |
| **Saga Pattern** | Choreography-based (no orchestrator — events drive state transitions) |
| **Project Path** | `src/Microservices/Ordering/` |

## Saga Flow

```
Cart Checkout
  └─► OrderSubmittedEvent
        └─► Ordering creates Order (status: Submitted)
              └─► ReserveInventoryEvent
                    ├─► InventoryReservedEvent ──► Order.MarkInventoryReserved()
                    │     └─► PaymentRequestedEvent
                    │           ├─► PaymentCompletedEvent ──► Order.MarkCompleted()
                    │           └─► PaymentFailedEvent ──► Order.MarkCancelled()
                    └─► InventoryReservationFailedEvent ──┘
```

**FastForwardTo()** handles race conditions where projection events arrive out of order.

## Key Domain Entities

| Entity | Type | Key Properties |
|:---|:---|:---|
| `Order` | Aggregate Root | BuyerId (string), Status, ShippingAddress (VO), Items, TotalAmount (computed) |
| `OrderItem` | Child Entity | **ProductId**, **SkuId**, **SkuCode**, ProductName, UnitPrice, Quantity, StoreId |
| `Address` | Value Object | Line1, Line2, City, State, PostalCode, Country |

### Order Status State Machine

```
Submitted → InventoryReserved → PaymentProcessing → Completed
    │              │                    │
    └──────────────┴────────────────────┴──► Cancelled / Faulted
```

Seller-managed path: `Submitted → Processing → Shipped → Delivered`

## API Endpoints (`/api/orders`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `POST` | `/` | `CreateOrderCommand` | Authenticated |
| `GET` | `/{id}` | `GetOrderByIdQuery` | Authenticated |
| `GET` | `/buyer/{buyerId}` | `ListOrdersByBuyerQuery` | Authenticated |
| `GET` | `/store/{storeId}` | `ListOrdersBySellerQuery` | Seller |
| `POST` | `/{id}/cancel` | `CancelOrderCommand` | Authenticated |
| `PUT` | `/{id}/status` | `UpdateOrderStatusCommand` | Seller |
| `GET` | `/has-purchased` | `HasPurchasedQuery` | Public (internal) |

## Integration Events

### Consumed

| Event | Consumer | Action |
|:---|:---|:---|
| `OrderSubmittedEvent` | `OrderSubmittedConsumer` | Creates Order from Cart checkout |
| `InventoryReservedEvent` | `OrderInventoryReservedConsumer` | Transitions order to InventoryReserved |
| `PaymentProcessingEvent` | `OrderPaymentProcessingConsumer` | Transitions order to PaymentProcessing |
| `PaymentCompletedEvent` | `OrderCompletedProjectionConsumer` | Transitions order to Completed |
| `OrderCancelledEvent` | `OrderCancelledProjectionConsumer` | Handles cancellation projection |
| `OrderStatusChangedEvent` | `OrderStatusProjectionConsumer` | General status sync |

### Published (via Outbox)

| Event | Trigger |
|:---|:---|
| `ReserveInventoryEvent` | Order created (Submitted) |
| `OrderStatusChangedEvent` | Any status transition |
| `OrderCompletedEvent` | Order reaches Completed/Delivered |
| `OrderCancelledEvent` | Order cancelled |
| `CancelOrderEvent` | Order cancellation (triggers inventory release + payment refund) |

## Current Status & Known Issues

- ✅ Choreography-based saga with full state machine
- ✅ `FastForwardTo()` handles out-of-order event delivery
- ✅ OrderItem is SKU-aware (SkuId, SkuCode)
- ✅ HasPurchased endpoint for review eligibility checks
- ⚠️ Saga compensation: if Payment fails, Inventory reservation is released and Order is cancelled
