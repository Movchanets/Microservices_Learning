# Phase 5 — Notification.Worker & Real-time Push

**Goal**: Deliver real-time push notifications to Angular clients via SignalR, backed by Redis for horizontal scaling.

## Sub-Plans

| # | File | Description |
|---|------|-------------|
| 5.0 | `5.0-notification-project-setup.md` | Create Notification.Worker project, add to solution, configure csproj |
| 5.1 | `5.1-signalr-hub-models.md` | SignalR Hub, IUserIdProvider, notification DTOs |
| 5.2 | `5.2-masstransit-consumers.md` | MassTransit consumers for Order, Payment, Inventory events |
| 5.3 | `5.3-program-cs-wiring.md` | Program.cs with SignalR + Redis backplane + MassTransit |
| 5.4 | `5.4-apphost-gateway-wiring.md` | Wire into AppHost, verify YARP routes for WebSocket |

## Dependencies
- Phase 4 completed (OrderCompletedEvent, OrderCancelledEvent, PaymentFailedEvent exist in SharedContracts)
- Phase 3 completed (InventoryReservationFailedEvent exists in SharedContracts)
- Redis resource already declared in AppHost
- RabbitMQ resource already declared in AppHost
- YARP route for `/hubs/notifications/**` already configured in gateway

## Architecture
```
Angular SPA ←WebSocket→ YARP Gateway ←→ Notification.Worker(s)
                                              ↕
                                        Redis Backplane
                                              ↕
                                        RabbitMQ (MassTransit)
```
