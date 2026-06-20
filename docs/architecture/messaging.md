# Messaging & Integration Events

> **Last Updated:** 2026-06-19

---

## Overview

All async inter-service communication uses **MassTransit** with the **Outbox pattern** for reliable delivery. The system uses **Automatonymous State Machines** for saga orchestration.

| Environment | Broker |
|:---|:---|
| Local (.NET Aspire) | RabbitMQ (Docker container) |
| Production (ACA) | Azure Service Bus (Standard/Premium) |

---

## Outbox Pattern

**Problem:** Dual-write — saving to DB and publishing to broker are two separate operations. If one fails, data becomes inconsistent.

**Solution:** MassTransit Outbox writes messages to a DB table in the **same transaction** as the business entity. A background process reliably delivers them to the broker.

```
Save Order + Outbox Message → Same DB Transaction
Background Worker → Reads Outbox → Publishes to RabbitMQ/ASB
```

This guarantees **at-least-once delivery** and eliminates the dual-write problem.

### Configuration Example

```csharp
services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<OrderDbContext>();
            r.UsePostgres();
        });

    x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
```

---

## Order Saga — State Machine

### States

| State | Description |
|:---|:---|
| `Initial` | Saga instance created, awaiting `OrderSubmittedEvent` |
| `ReservingInventory` | `ReserveInventoryCommand` sent, awaiting stock confirmation |
| `ProcessingPayment` | Inventory reserved, payment command sent |
| `Completed` | Payment succeeded, `OrderPublishedEvent` emitted |
| `Cancelled` | Compensation applied after payment failure |
| `Faulted` | Unrecoverable error (e.g., inventory depletion) |

### Happy Path Flow

```
OrderSubmittedEvent → ReservingInventory
    → InventoryReservedEvent → ProcessingPayment
        → PaymentCompletedEvent → Completed
```

### Compensation Flow (Payment Failed)

```
PaymentFailedEvent → Compensating
    → CancelReservationCommand → InventoryReleasedEvent → Cancelled
```

### State Machine Definition

```csharp
public sealed class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State ReservingInventory { get; private set; } = null!;
    public State ProcessingPayment { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;
    public State Faulted { get; private set; } = null!;

    public Event<OrderSubmittedEvent> OrderSubmitted { get; private set; } = null!;
    public Event<InventoryReservedEvent> InventoryReserved { get; private set; } = null!;
    public Event<InventoryReservationFailedEvent> InventoryFailed { get; private set; } = null!;
    public Event<PaymentCompletedEvent> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmitted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => InventoryReserved, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PaymentCompleted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PaymentFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        Initially(
            When(OrderSubmitted)
                .Then(ctx => { /* populate state */ })
                .Publish(ctx => new ReserveInventoryCommand(ctx.Saga.CorrelationId, ctx.Saga.Items))
                .TransitionTo(ReservingInventory));

        During(ReservingInventory,
            When(InventoryReserved)
                .Publish(ctx => new ProcessPaymentCommand(ctx.Saga.CorrelationId, ctx.Saga.TotalAmount))
                .TransitionTo(ProcessingPayment),
            When(InventoryFailed)
                .TransitionTo(Faulted));

        During(ProcessingPayment,
            When(PaymentCompleted)
                .Publish(ctx => new OrderCompletedEvent(ctx.Saga.CorrelationId))
                .TransitionTo(Completed),
            When(PaymentFailed)
                .Publish(ctx => new CancelReservationCommand(ctx.Saga.CorrelationId))
                .TransitionTo(Cancelled));
    }
}
```

---

## Integration Event Contracts

All contracts live in `src/BuildingBlocks/SharedContracts/`.

### Events (SharedContracts)

| Event | Published By | Consumers |
|:---|:---|:---|
| `OrderSubmittedEvent` | Cart.API | Ordering (Saga) |
| `OrderCompletedEvent` | Ordering (Saga) | Notification.Worker |
| `InventoryReservedEvent` | Inventory.API | Ordering (Saga) |
| `InventoryReservationFailedEvent` | Inventory.API | Ordering (Saga) |
| `InventoryReleasedEvent` | Inventory.API | — |
| `PaymentCompletedEvent` | Payment.API | Ordering (Saga) |
| `PaymentFailedEvent` | Payment.API | Ordering (Saga), Notification.Worker |
| `ProductCreatedEvent` | Catalog.API | Search, Cart, Inventory (legacy) |
| `ProductUpdatedEvent` | Catalog.API | Search, Cart |
| `ProductDeletedEvent` | Catalog.API | Search, Cart |
| `SkuCreatedIntegrationEvent` | Catalog.API | Inventory |
| `SkuDeletedEvent` | **NEVER PUBLISHED** | — |
| `SkuPriceChangedEvent` | **NEVER PUBLISHED** | — |
| `ProductPriceChangedEvent` | **NEVER PUBLISHED** | — |
| `StoreCreatedIntegrationEvent` | StoreManagement.API | — |
| `StoreVerifiedIntegrationEvent` | StoreManagement.API | — |
| `PasswordResetRequestedIntegrationEvent` | Identity.API | — |

### Commands (SharedContracts)

| Command | Sent By | Handled By |
|:---|:---|:---|
| `ReserveInventoryCommand` | Ordering (Saga) | Inventory.API |
| `CancelReservationCommand` | Ordering (Saga) | Inventory.API |
| `ProcessPaymentCommand` | Ordering (Saga) | Payment.API |

---

## Event Flow Matrix — Current vs Expected

> ⚠️ **Source:** `DDD_EventAlignment_Audit.md` (2026-05-25)

| Event | Published? | Inventory Consumer | Search Consumer | Cart Consumer |
|:---|:---:|:---:|:---:|:---:|
| `ProductCreatedEvent` | ✅ | ✅ (legacy) | ✅ | ✅ (skips empty SKU) |
| `ProductUpdatedEvent` | ✅ | ✅ (legacy) | ✅ | ✅ (stale price risk) |
| `ProductDeletedEvent` | ✅ | ❌ | ✅ | ✅ |
| `SkuCreatedIntegrationEvent` | ✅ | ✅ | ❌ MISSING | ❌ MISSING |
| `SkuDeletedEvent` | ❌ NEVER PUBLISHED | ❌ MISSING | ❌ MISSING | ❌ MISSING |
| `SkuPriceChangedEvent` | ❌ NEVER PUBLISHED | ❌ MISSING | ❌ MISSING | ❌ MISSING |
| `ProductPriceChangedEvent` | ❌ NEVER PUBLISHED | — | — | ✅ (handler exists, never fires) |

---

## Critical Event Gaps

### 🔴 SkuDeletedDomainEvent — No Handler

`Product.RemoveSku()` raises `SkuDeletedDomainEvent` but no handler translates it to `SkuDeletedEvent`. Downstream impact:
- Inventory never deactivates the InventoryItem → phantom stock remains reservable
- Search never removes deleted SKUs from index
- Cart never invalidates carts containing deleted SKUs

### 🔴 Sku.ChangePrice() — No Domain Event

`Sku.ChangePrice()` updates the Price property but does NOT raise a domain event. Downstream impact:
- Cart prices go stale after price changes
- Search index shows outdated prices
- No audit trail of price changes

### 🟠 Cart Is Product-Level Only

Cart data model is keyed by `ProductId`, not `SkuId`. With multi-SKU products, Cart cannot distinguish between variants.

### 🟠 Search Index Is Product-Level Only

`ProductSearchDocument` stores one Price, one Sku code, one set of Attributes. Multi-SKU products only show one variant's data.

---

## Architecture Principle Violations

| Principle | Violation |
|:---|:---|
| Event-driven consistency | Domain events raised but not translated to integration events (SkuDeleted, PriceChanged) |
| Database-per-service | Cart keys by ProductId — leaks Catalog's old model into Cart's schema |
| Saga compensation | Inventory reservation by ProductId can't compensate per-SKU |
| Consumer completeness | Every integration event should have ALL relevant consumers — gaps exist |
| Single source of truth | Price lives on SKU in Catalog, but ProductPrice in Cart is Product-level |
| Aggregate boundary | CartItem references ProductId (Catalog aggregate) without SkuId — incomplete reference |
