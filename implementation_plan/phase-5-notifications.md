# Phase 5 — Notification.Worker & Real-time Push

**Goal**: Deliver real-time push notifications to Angular clients via SignalR, backed by Redis for horizontal scaling.

**Depends on**: Phase 4

## Tasks

- [ ] **Create `Notification.Worker` project** in `src/Microservices/Notification/Notification.Worker/`
  - Background worker service (no Minimal API endpoints needed for public access)
  - SignalR Hub: `NotificationHub` at `/hubs/notifications`
- [ ] **Configure SignalR with Redis Backplane**
  - `AddSignalR().AddStackExchangeRedis(...)` — all instances share connections via Redis pub/sub
- [ ] **Implement MassTransit consumers**
  - `OrderCompletedConsumer` → Push "Order confirmed" to buyer via SignalR
  - `PaymentFailedConsumer` → Push "Payment declined" to buyer
  - `InventoryReservationFailedConsumer` → Push "Item out of stock" to buyer
  - Extensible pattern for future event types
- [ ] **Implement user-targeted delivery**
  - Map `BuyerId` → SignalR connection using `IUserIdProvider`
  - `hubContext.Clients.User(buyerId).SendAsync("OrderUpdate", payload)`
- [ ] **Configure YARP WebSocket routing**
  - Route `/hubs/notifications/**` → Notification.Worker cluster
  - Enable Session Affinity (sticky sessions) with `HashCookie` policy
- [ ] **Register in AppHost** with `redis` and `messaging` references
- [ ] **Write integration tests**
  - Publish `OrderCompletedEvent` → verify SignalR client receives notification
  - Test with multiple worker instances (Redis backplane routing)

## Deliverables
```
src/Microservices/
└── Notification/
    └── Notification.Worker/
```
