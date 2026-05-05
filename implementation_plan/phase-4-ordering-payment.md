# Phase 4 — Ordering Saga & Payment.API

**Goal**: Implement the core order lifecycle using MassTransit State Machine saga with compensating transactions, and the payment gateway integration.

**Depends on**: Phase 3

> ⚠️ **This is the most complex phase** — the Ordering saga orchestrates Inventory + Payment with full compensation support.

## Ordering.API Tasks

- [ ] **Scaffold Clean Architecture projects**
  - `Ordering.Domain/` — Order aggregate, OrderItem entity, Address value object, OrderStatus enum
  - `Ordering.Application/` — CreateOrder, CancelOrder, GetOrderById, ListOrdersByBuyer
  - `Ordering.Infrastructure/` — EF Core DbContext with `ordering-db`, migrations, saga state persistence
  - `Ordering.API/` — Minimal API endpoints for order queries
- [ ] **Implement `OrderStateMachine`** (MassTransit Automatonymous)
  - States: `ReservingInventory`, `ProcessingPayment`, `Completed`, `Cancelled`, `Faulted`
  - Events: `OrderSubmitted`, `InventoryReserved`, `InventoryFailed`, `PaymentCompleted`, `PaymentFailed`
  - Happy path: Submitted → ReserveInventory → ProcessPayment → Completed
  - Compensation: PaymentFailed → CancelReservation → Cancelled
- [ ] **Implement `OrderState`** saga instance with EF Core persistence
  - CorrelationId, CurrentState, BuyerId, Items, TotalAmount, CreatedAt
  - Use C# 14 `field` keyword for validated properties
- [ ] **Define remaining integration contracts** in `SharedContracts`
  - `ProcessPaymentCommand(Guid CorrelationId, decimal Amount, string BuyerId)`
  - `PaymentCompletedEvent(Guid CorrelationId, string TransactionId)`
  - `PaymentFailedEvent(Guid CorrelationId, string FailureReason)`
  - `OrderCompletedEvent(Guid CorrelationId, string BuyerId)`
  - `OrderCancelledEvent(Guid CorrelationId, string Reason)`
- [ ] **Configure MassTransit** — Saga registration with EF Core repository + Outbox
- [ ] **Add YARP route** `/api/orders/**` → Ordering.API
- [ ] **Register in AppHost** with `ordering-db` and `messaging`
- [ ] **Write unit tests** — State machine transition logic (mock events)
- [ ] **Write integration tests** — Full saga flow with Testcontainers (PostgreSQL + RabbitMQ)

## Payment.API Tasks

- [ ] **Scaffold Clean Architecture projects**
  - `Payment.Domain/` — PaymentTransaction aggregate, PaymentStatus enum
  - `Payment.Application/` — ProcessPayment handler, RefundPayment handler
  - `Payment.Infrastructure/` — EF Core DbContext with `payment-db`, external gateway client (Stripe SDK)
  - `Payment.API/` — Minimal API for payment status queries + webhook endpoint
- [ ] **Implement MassTransit consumer**
  - `ProcessPaymentConsumer` — Call external gateway, publish `PaymentCompletedEvent` or `PaymentFailedEvent`
- [ ] **Implement webhook endpoint** — Receive payment gateway callbacks (Stripe/PayPal)
- [ ] **Configure MassTransit Outbox** for reliable event publishing
- [ ] **Add YARP route** `/api/payments/**` → Payment.API
- [ ] **Register in AppHost** with `payment-db` and `messaging`
- [ ] **Write integration tests** — Payment consumer with mocked gateway

## Saga Verification Checklist

- [ ] Happy path: Cart checkout → Inventory reserved → Payment completed → Order completed
- [ ] Compensation: Cart checkout → Inventory reserved → Payment failed → Inventory released → Order cancelled
- [ ] Idempotency: Duplicate events don't create duplicate state transitions
- [ ] Outbox: Events survive process crash (kill service mid-transaction, verify delivery after restart)

## Deliverables
```
src/Microservices/
├── Ordering/
│   ├── Ordering.Domain/
│   ├── Ordering.Application/
│   ├── Ordering.Infrastructure/
│   └── Ordering.API/
└── Payment/
    ├── Payment.Domain/
    ├── Payment.Application/
    ├── Payment.Infrastructure/
    └── Payment.API/
```
