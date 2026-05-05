# Phase 3 — Inventory.API & Cart.API

**Goal**: Build stock reservation with optimistic locking and the Redis-backed shopping cart.

**Depends on**: Phase 2

## Inventory.API Tasks

- [ ] **Scaffold Clean Architecture projects**
  - `Inventory.Domain/` — InventoryItem aggregate (SKU, AvailableQuantity, ReservedQuantity), Reservation entity
  - `Inventory.Application/` — ReserveStock, ReleaseStock, GetStockBySku commands/queries
  - `Inventory.Infrastructure/` — EF Core DbContext with `inventory-db`, optimistic concurrency (row versioning)
  - `Inventory.API/` — Minimal API endpoints for stock queries
- [ ] **Define integration event contracts** in `SharedContracts`
  - `ReserveInventoryCommand(Guid CorrelationId, List<OrderItemContract> Items)`
  - `CancelReservationCommand(Guid CorrelationId)`
  - `InventoryReservedEvent(Guid CorrelationId)`
  - `InventoryReservationFailedEvent(Guid CorrelationId, string Reason)`
  - `InventoryReleasedEvent(Guid CorrelationId)`
- [ ] **Implement MassTransit consumers**
  - `ReserveInventoryConsumer` — Check stock, apply optimistic lock, create reservation record
  - `CancelReservationConsumer` — Find reservation by CorrelationId, release stock
- [ ] **Configure MassTransit Outbox** for reliable event publishing
- [ ] **Add YARP route** `/api/inventory/**` → Inventory.API
- [ ] **Register in AppHost** with `inventory-db` and `messaging`
- [ ] **Write unit tests** — Overselling prevention, reservation/release logic
- [ ] **Write integration tests** — Concurrent reservation race condition (optimistic locking)

## Cart.API Tasks

- [ ] **Create Cart.API project** in `src/Microservices/Cart/Cart.API/`
  - Stateless service — no Domain/Application/Infrastructure layers
  - Direct Redis operations via `IDistributedCache` or `StackExchange.Redis`
- [ ] **Implement endpoints**
  - `GET /api/cart` — Get current user's cart
  - `POST /api/cart/items` — Add item to cart
  - `PUT /api/cart/items/{id}` — Update quantity
  - `DELETE /api/cart/items/{id}` — Remove item
  - `POST /api/cart/checkout` — Submit cart as order, publish `OrderSubmittedEvent`, delete cart from Redis
- [ ] **Define `OrderSubmittedEvent`** in `SharedContracts`
  - `OrderSubmittedEvent(Guid CorrelationId, string BuyerId, List<OrderItemContract> Items, DateTime Timestamp)`
- [ ] **Add YARP route** `/api/cart/**` → Cart.API
- [ ] **Register in AppHost** with `redis` and `messaging`
- [ ] **Write integration tests** — Add/remove items, checkout flow

## Deliverables
```
src/Microservices/
├── Inventory/
│   ├── Inventory.Domain/
│   ├── Inventory.Application/
│   ├── Inventory.Infrastructure/
│   └── Inventory.API/
└── Cart/
    └── Cart.API/
```
