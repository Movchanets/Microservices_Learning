# Phase 4 — Findings

## Architecture Notes
- OrderStateMachine is the most complex component — orchestrates Inventory + Payment with full compensation
- Saga states: Initial → ReservingInventory → ProcessingPayment → Completed/Cancelled/Faulted
- Compensation path: PaymentFailed → CancelReservation (Inventory) → Cancelled
- Payment service follows full Clean Architecture (not thin) — needs transaction persistence for audit

## MassTransit v8 Patterns (from Catalog reference)
- `x.SetKebabCaseEndpointNameFormatter()` — endpoint naming
- `x.AddSagaStateMachine<OrderStateMachine, OrderState>().EntityFrameworkRepository(r => { r.ExistingDbContext<T>(); r.UsePostgres(); })` — saga persistence
- `x.AddEntityFrameworkOutbox<TDbContext>(o => { o.UsePostgres(); o.UseBusOutbox(); })` — outbox
- `cfg.Host(builder.Configuration.GetConnectionString("messaging"))` — RabbitMQ host
- `cfg.ConfigureEndpoints(context)` — auto-configure endpoints

## Existing Infrastructure
- `ordering-db` and `payment-db` databases already declared in AppHost.cs
- YARP routes for `/api/orders/**` and `/api/payments/**` already in gateway `appsettings.json`
- Phase 4 comment placeholders in AppHost.cs at lines 99-100
- `BuildingBlocks.Infrastructure` has `GlobalExceptionMiddleware`, `ValidationBehavior`, `LoggingBehavior`

## Existing Contracts (from Phase 3)
- `OrderSubmittedEvent` (Cart → Ordering) — in `Events/Cart/`
- `ReserveInventoryCommand` / `CancelReservationCommand` (Ordering → Inventory) — in `Commands/Inventory/`
- `InventoryReservedEvent` / `InventoryReservationFailedEvent` / `InventoryReleasedEvent` (Inventory → Ordering) — in `Events/Inventory/`
- `OrderItemContract` (shared DTO) — in `Dtos/`

## Contracts to Create in Phase 4
- `ProcessPaymentCommand` (Ordering → Payment) — in `Commands/Payment/`
- `PaymentCompletedEvent` / `PaymentFailedEvent` (Payment → Ordering) — in `Events/Payment/`
- `OrderCompletedEvent` / `OrderCancelledEvent` (Ordering → Notification) — in `Events/Ordering/`

## Saga State Machine Design
- `OrderState` stores `ItemsJson` as serialized JSON for simplicity
- `CorrelationId` = `OrderId` (simplifies correlation)
- Saga publishes commands to Inventory and Payment via `.Publish()`
- Compensation: When `PaymentFailedEvent` arrives during `ProcessingPayment`, saga publishes `CancelReservationCommand` and transitions to `Cancelled`

## Payment Gateway Strategy
- Mock gateway for local development (always succeeds)
- IPaymentGateway interface for future Stripe/PayPal integration
- Gateway called inside `ProcessPaymentConsumer`, not in the domain
