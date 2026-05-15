# Phase 5 — Task Plan

## Goal
Implement Notification.Worker with SignalR hub + Redis backplane + MassTransit consumers for real-time order status push to Angular clients.

## Sub-Plans
| # | Status | Description |
|---|--------|-------------|
| 5.0 | pending | Project setup — create Notification.Worker, add to solution |
| 5.1 | pending | SignalR Hub, UserIdProvider, notification DTOs |
| 5.2 | pending | MassTransit consumers (4 events) |
| 5.3 | pending | Program.cs wiring — SignalR + Redis + MassTransit |
| 5.4 | pending | AppHost + Gateway wiring, verify YARP WebSocket routes |

## Key Decisions
- Worker Service template (not Web API) — no REST endpoints, only SignalR hub
- Redis Backplane for horizontal scaling of SignalR
- `BuyerIdUserIdProvider` maps `x-buyer-id` header → SignalR user targeting
- `PaymentFailedEvent` and `InventoryReservationFailedEvent` lack `BuyerId` — broadcast to all or add contract field

## Contract Gap
`PaymentFailedEvent` and `InventoryReservationFailedEvent` don't carry `BuyerId`. Options:
1. Broadcast to all clients (simple)
2. Query Ordering API (adds HTTP dependency)
3. Add `BuyerId` to contracts (recommended — Phase 4 contract update)
