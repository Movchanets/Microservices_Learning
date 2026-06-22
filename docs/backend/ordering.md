# Ordering Service

> **Last Updated:** 2026-06-20

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Full 4-layer (Domain → Application → Infrastructure → API) |
| **Database** | PostgreSQL (EF Core + Npgsql) |
| **Messaging** | RabbitMQ via MassTransit (with EF Outbox) |
| **Saga Pattern** | Orchestration-based (`MassTransitStateMachine<OrderState>`) |
| **Project Path** | `src/Microservices/Ordering/` |

## Saga Flow

The `OrderStateMachine` orchestrates the multi-service order lifecycle:

```
Cart Checkout
  └─► OrderSubmittedEvent
        └─► Saga stores order data, publishes ReserveInventoryCommand
              └─► ReservingInventory state
                    ├─► InventoryReservedEvent
                    │     └─► Saga publishes ProcessPaymentCommand + OrderStatusChangedEvent("PaymentProcessing")
                    │           └─► ProcessingPayment state
                    │                 ├─► PaymentCompletedEvent
                    │                 │     └─► Saga publishes OrderCompletedEvent → Completed
                    │                 ├─► PaymentFailedEvent
                    │                 │     └─► Saga publishes RefundPaymentIntegrationCommand + CancelReservationCommand + OrderCancelledEvent → Cancelled
                    │                 └─► CancelOrderEvent (buyer-initiated)
                    │                       └─► Saga publishes RefundPaymentIntegrationCommand + CancelReservationCommand + OrderCancelledEvent → Cancelled
                    ├─► InventoryReservationFailedEvent
                    │     └─► Saga publishes OrderCancelledEvent → Faulted
                    └─► CancelOrderEvent (buyer-initiated)
                          └─► Saga publishes CancelReservationCommand + OrderCancelledEvent → Cancelled
```

**Compensation paths:**
- **Payment fails**: Refund payment + release inventory + cancel order
- **Buyer cancels during inventory reservation**: Release inventory + cancel order
- **Buyer cancels during payment processing**: Refund payment + release inventory + cancel order
- **Inventory reservation fails**: Cancel order (Faulted)

**FastForwardTo()** on the Order entity handles race conditions where projection events arrive out of order (e.g., `ProcessPaymentCommand` arrives before `InventoryReservedEvent`).

## Key Domain Entities

| Entity | Type | Key Properties |
|:---|:---|:---|
| `Order` | Aggregate Root | BuyerId (string), Status, ShippingAddress (VO), Items, TotalAmount (computed), CreatedAt, CompletedAt, CancellationReason |
| `OrderItem` | Child Entity | ProductId, SkuId, SkuCode, ProductName, UnitPrice, Quantity, StoreId |
| `Address` | Value Object | Street, City, State, Country, ZipCode |

### Order Status Enum

| Value | Int | Description |
|:---|:---:|:---|
| `Submitted` | 0 | Order created from cart checkout |
| `InventoryReserved` | 1 | Stock allocated for order items |
| `PaymentProcessing` | 2 | Payment gateway processing |
| `Completed` | 3 | Payment successful, order fulfilled |
| `Cancelled` | 4 | Terminal — cancelled by buyer or compensation |
| `Faulted` | 5 | Terminal — inventory reservation failed |
| `Processing` | 6 | Seller fulfillment: preparing shipment |
| `Shipped` | 7 | Seller fulfillment: shipped to buyer |
| `Delivered` | 8 | Seller fulfillment: received by buyer |

**Saga path**: `Submitted → InventoryReserved → PaymentProcessing → Completed`
**Seller fulfillment path**: `Submitted → Processing → Shipped → Delivered`
**Terminal states**: `Cancelled`, `Faulted`

## API Endpoints (`/api/orders`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `POST` | `/` | `CreateOrderCommand` | Authenticated (buyer from JWT) |
| `GET` | `/{id:guid}` | `GetOrderByIdQuery` | Authenticated |
| `GET` | `/buyer/{buyerId}` | `ListOrdersByBuyerQuery` | Authenticated |
| `GET` | `/store/{storeId:guid}` | `ListOrdersBySellerQuery` | Seller |
| `POST` | `/{id:guid}/cancel` | `CancelOrderCommand` | Authenticated (buyer from JWT, body: `{ reason? }`) |
| `PUT` | `/{id:guid}/status` | `UpdateOrderStatusCommand` | Seller (body: `{ status, notes? }`) |
| `GET` | `/has-purchased` | `HasPurchasedQuery` | AllowAnonymous (internal service call, query: `buyerId`, `productId`) |

## Integration Events

### Consumed

| Event | Consumer | Action |
|:---|:---|:---|
| `OrderSubmittedEvent` | `OrderSubmittedConsumer` | Creates Order aggregate from cart checkout data |
| `InventoryReservedEvent` | `OrderInventoryReservedConsumer` | Transitions order to InventoryReserved |
| `ProcessPaymentCommand` | `OrderPaymentProcessingConsumer` | Transitions order to PaymentProcessing (fast-forwards if needed) |
| `OrderCompletedEvent` | `OrderCompletedProjectionConsumer` | Transitions order to Completed |
| `OrderCancelledEvent` | `OrderCancelledProjectionConsumer` | Transitions order to Cancelled |
| `OrderStatusChangedEvent` | `OrderStatusProjectionConsumer` | General status sync (used by saga and domain consumers) |

### Published (via Outbox)

| Event | Trigger |
|:---|:---|
| `OrderStatusChangedEvent` | Any status transition (from domain events and saga) |
| `OrderCompletedEvent` | Order reaches Completed or Delivered |
| `OrderCancelledEvent` | Order cancelled (from saga compensation or buyer action) |
| `ReserveInventoryCommand` | Saga: order submitted → request inventory reservation |
| `ProcessPaymentCommand` | Saga: inventory reserved → request payment processing |
| `CancelReservationCommand` | Saga: compensation — release inventory on cancellation/failure |
| `RefundPaymentIntegrationCommand` | Saga: compensation — refund payment on cancellation/failure |

## Current Status & Known Issues

- ✅ Orchestration-based saga (`MassTransitStateMachine`) with full state machine
- ✅ `FastForwardTo()` handles out-of-order event delivery in projection consumers
- ✅ OrderItem is SKU-aware (SkuId, SkuCode)
- ✅ HasPurchased endpoint for review eligibility checks
- ✅ Seller fulfillment path (Processing → Shipped → Delivered)
- ✅ Compensation: payment failure triggers refund + inventory release + cancellation
- ✅ Buyer can cancel during any non-terminal state
