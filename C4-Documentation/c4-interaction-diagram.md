# Microservice Interaction Diagram — Marketplace Platform

## Overview

This document maps every service-to-service interaction in the Marketplace platform. Communication falls into two categories:

1. **Asynchronous (MassTransit / RabbitMQ)** — Event-driven, eventual consistency, saga orchestration
2. **Synchronous (HTTP via YARP)** — Request/response through API Gateway

---

## Diagram 1: Checkout Saga Flow (Success Path)

```mermaid
%%{init: {'sequence': {'mirrorActors': false, 'actorMargin': 80, 'messageMargin': 50, 'width': 200}}}%%
sequenceDiagram
    participant Cart
    participant Saga as Ordering<br/>Saga
    participant Inv as Inventory
    participant Pay as Payment
    participant Notif as Notification

    Cart->>Saga: OrderSubmitted
    activate Saga
    Saga->>Inv: ReserveInventory
    activate Inv
    Inv-->>Saga: Reserved
    deactivate Inv

    Saga->>Pay: ProcessPayment
    activate Pay
    Pay-->>Saga: Completed
    deactivate Pay

    Saga->>Notif: OrderCompleted
    deactivate Saga
    Notif-->>Notif: SignalR push
```

---

## Diagram 2: Checkout Saga (Compensation / Failure)

```mermaid
%%{init: {'sequence': {'mirrorActors': false, 'actorMargin': 80, 'messageMargin': 50, 'width': 200}}}%%
sequenceDiagram
    participant Cart
    participant Saga as Ordering<br/>Saga
    participant Inv as Inventory
    participant Pay as Payment
    participant Notif as Notification

    Cart->>Saga: OrderSubmitted
    activate Saga
    Saga->>Inv: ReserveInventory
    Inv-->>Saga: Reserved

    Saga->>Pay: ProcessPayment
    Pay-->>Saga: Failed

    Saga->>Inv: CancelReservation
    Saga->>Notif: OrderCancelled
    deactivate Saga
    Notif-->>Notif: SignalR push
```

---

## Diagram 3: Buyer-Initiated Cancellation (Plan 11)

```mermaid
%%{init: {'sequence': {'mirrorActors': false, 'actorMargin': 80, 'messageMargin': 50, 'width': 200}}}%%
sequenceDiagram
    participant Buyer
    participant Handler as CancelOrder<br/>Handler
    participant Saga as Ordering<br/>Saga
    participant Inv as Inventory
    participant Notif as Notification

    Buyer->>Handler: POST /orders/{id}/cancel
    Handler->>Saga: CancelOrderEvent
    activate Saga
    Saga->>Inv: CancelReservation
    Saga->>Notif: OrderCancelled
    deactivate Saga
    Notif-->>Notif: SignalR push
```

---

## Diagram 4: Product Event Fan-Out

```mermaid
%%{init: {'flowchart': {'curve': 'basis'}}}%%
graph LR
    CAT["Catalog"]

    subgraph Events
        PC["Created"]
        PU["Updated"]
        PP["PriceChanged"]
        PD["Deleted"]
    end

    SCH["Search"]
    CRT["Cart"]
    INV["Inventory"]

    CAT --> PC & PU & PP & PD

    PC --> SCH
    PU --> SCH
    PD --> SCH

    PC --> CRT
    PU --> CRT
    PP --> CRT
    PD --> CRT

    PC --> INV
```

| Event | Search | Cart | Inventory |
|-------|--------|------|-----------|
| ProductCreated | Index in ES | Sync price | Init stock (0) |
| ProductUpdated | Reindex | Sync price | — |
| ProductPriceChanged | — | Update price | — |
| ProductDeleted | Remove | Remove price | — |

---

## Diagram 5: Store → Identity Role Promotion

```mermaid
%%{init: {'flowchart': {'curve': 'basis'}}}%%
graph LR
    Admin["Admin"]
    STR["StoreMgmt"]
    ID["Identity"]

    Admin --> STR
    STR --> ID
```

| Step | Action |
|------|--------|
| 1 | Admin: `POST /api/stores/{id}/verify` |
| 2 | StoreMgmt publishes `StoreVerifiedIntegrationEvent` |
| 3 | Identity consumer promotes user: Buyer → Seller |

---

## Diagram 6: Gateway Routing

```mermaid
%%{init: {'flowchart': {'curve': 'basis'}}}%%
graph TB
    SPA["Angular SPA"]

    GW["API Gateway<br/>YARP + BFF"]

    ID["Identity"]
    CAT["Catalog"]
    SCH["Search"]
    CRT["Cart"]
    ORD["Ordering"]
    INV["Inventory"]
    PAY["Payment"]
    NOT["Notification"]
    STR["StoreMgmt"]
    MED["Media"]

    SPA --> GW

    GW --> ID
    GW --> CAT
    GW --> SCH
    GW --> CRT
    GW --> ORD
    GW --> INV
    GW --> PAY
    GW --> NOT
    GW --> STR
    GW --> MED
```

| Route | Target | Auth |
|-------|--------|------|
| `/api/identity/**` | Identity | Public + Auth |
| `/api/catalog/**` | Catalog | Public + Auth |
| `/api/search/**` | Search | Public |
| `/api/cart/**` | Cart | Auth |
| `/api/orders/**` | Ordering | Auth |
| `/api/inventory/**` | Inventory | Auth |
| `/api/payments/**` | Payment | Auth |
| `/hubs/**` | Notification | SignalR |
| `/api/stores/**` | StoreMgmt | Public + Auth |
| `/api/media/**` | Media | Public + Auth |

---

## Diagram 7: Data Store Mapping

```mermaid
%%{init: {'flowchart': {'curve': 'basis'}}}%%
graph LR
    subgraph Services
        ID["Identity"]
        CAT["Catalog"]
        ORD["Ordering"]
        INV["Inventory"]
        CRT["Cart"]
        PAY["Payment"]
        STR["StoreMgmt"]
        SCH["Search"]
        MED["Media"]
        NOT["Notification"]
    end

    subgraph PostgreSQL
        PG[("7 databases")]
    end

    subgraph Other
        ES[("Elasticsearch")]
        BLOB[("Blob Storage")]
        REDIS[("Redis")]
    end

    ID & CAT & ORD & INV & CRT & PAY & STR --> PG
    SCH --> ES
    MED --> BLOB
    CRT & NOT --> REDIS
```

| Service | Store | Database |
|---------|-------|----------|
| Identity | PostgreSQL | identity-db |
| Catalog | PostgreSQL | catalog-db |
| Ordering | PostgreSQL | ordering-db + saga state |
| Inventory | PostgreSQL | inventory-db |
| Cart | PostgreSQL + Redis | cart-db + cache |
| Payment | PostgreSQL | payment-db |
| StoreMgmt | PostgreSQL | store-db |
| Search | Elasticsearch | marketplace-products |
| Media | Azure Blob | media container |
| Notification | Redis | SignalR backplane |

---

## Full Event/Command Reference

### Commands (point-to-point)

| Command | Producer | Consumer | Purpose |
|---------|----------|----------|---------|
| ReserveInventoryCommand | Ordering | Inventory | Reserve stock |
| CancelReservationCommand | Ordering | Inventory | Release stock |
| ProcessPaymentCommand | Ordering | Payment | Process payment |

### Events (pub/sub)

| Event | Producer | Consumers |
|-------|----------|-----------|
| OrderSubmitted | Cart | Ordering |
| InventoryReserved | Inventory | Ordering |
| InventoryFailed | Inventory | Ordering |
| InventoryReleased | Inventory | *(none)* |
| PaymentCompleted | Payment | Ordering |
| PaymentFailed | Payment | Ordering |
| OrderCompleted | Ordering | Notification |
| OrderCancelled | Ordering | Notification |
| CancelOrder | Ordering (handler) | Ordering (saga) |
| OrderStatusChanged | Ordering | Notification |
| ProductCreated | Catalog | Search, Cart, Inventory |
| ProductUpdated | Catalog | Search, Cart |
| ProductPriceChanged | Catalog | Cart |
| ProductDeleted | Catalog | Search, Cart |
| UserRegistered | Identity | *(none)* |
| StoreVerified | StoreMgmt | Identity |

---

## Interaction Patterns

| Pattern | Where | Description |
|---------|-------|-------------|
| Saga Orchestrator | Ordering → Inventory → Payment | Multi-step with compensation |
| Fan-Out | Catalog → Search, Cart, Inventory | One event, three consumers |
| CQRS Projection | Ordering saga → projections | Saga state → read model |
| Outbox | All publishers | EF Core Outbox + RabbitMQ |
| BFF | API Gateway | Cookie-to-bearer auth |
| Compensation | Saga on failure/cancel | CancelReservationCommand |

---

## Related Documentation

- [Context Diagram](c4-context.md) — System context with personas
- [Container Diagram](c4-container.md) — Deployment containers and APIs
- [Component Index](c4-component.md) — All 31 components
