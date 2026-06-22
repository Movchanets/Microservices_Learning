# Notification Service

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Thin (Worker — no API endpoints, no Domain layer) |
| **Transport** | SignalR Hub with Redis backplane |
| **Messaging** | RabbitMQ via MassTransit (consumers only) |
| **Project Path** | `src/Microservices/Notification/Notification.Worker/` |

## Architecture

```
RabbitMQ ──► MassTransit Consumers ──► SignalR HubContext ──► Redis Backplane ──► Browser WebSocket
```

- **Hub:** `NotificationHub` (mapped at `/hubs/notifications` via Gateway)
- **Backplane:** StackExchange.Redis for multi-instance scaling
- **No database** — purely a message relay

## Consumers

| Consumer | Event | SignalR Method |
|:---|:---|:---|
| `OrderStatusChangedConsumer` | `OrderStatusChangedEvent` | `Clients.User(buyerId).SendAsync("OrderUpdate", ...)` |
| `OrderCompletedConsumer` | `OrderCompletedEvent` | `Clients.User(buyerId).SendAsync("OrderUpdate", ...)` |
| `OrderCancelledConsumer` | `OrderCancelledEvent` | `Clients.User(buyerId).SendAsync("OrderUpdate", ...)` |

### Message Format

```csharp
record OrderUpdateMessage(Guid OrderId, string BuyerId, string Status, string? Reason, DateTime Timestamp);
```

## Integration Events

### Consumed

| Event | Source | Action |
|:---|:---|:---|
| `OrderStatusChangedEvent` | Ordering | Push order status update to buyer |
| `OrderCompletedEvent` | Ordering | Push order completion notification |
| `OrderCancelledEvent` | Ordering | Push cancellation notification |

### Published

None — Notification is a terminal consumer.

## Current Status & Known Issues

- ✅ Real-time order status notifications via SignalR
- ✅ Redis backplane for horizontal scaling
- ✅ Buyer-targeted delivery (user ID-based)
- ⚠️ No email/SMS fallback for offline users
- ⚠️ No notification persistence (missed if disconnected)
- ⚠️ Scalar container failed to start (non-blocking, docs only)

---

*Last Updated: 2026-06-19*
