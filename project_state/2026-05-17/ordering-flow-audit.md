# Ordering Flow Audit — 2026-05-17

## Scope

- Microservices reviewed: `Cart`, `Ordering`, `Inventory`, `Payment`, `Notification.Worker`, `ApiGateway`, `web`
- Focus: cart add/update, checkout, order creation, payment processing, SignalR notifications

## Event / Consumer Map

- `OrderSubmittedEvent`
  - Produced by: `Cart.Application/CheckoutCartCommandHandler`
  - Consumed by: `Ordering.API/Saga/OrderStateMachine`, `Ordering.Infrastructure/Messaging/Consumers/OrderSubmittedConsumer`
- `ReserveInventoryCommand`
  - Produced by: `Ordering.API/Saga/OrderStateMachine`
  - Consumed by: `Inventory.Infrastructure/Messaging/Consumers/ReserveInventoryConsumer`
- `InventoryReservedEvent`
  - Produced by: `Inventory.Infrastructure/Messaging/Consumers/ReserveInventoryConsumer`
  - Consumed by: `Ordering.API/Saga/OrderStateMachine`, `Ordering.Infrastructure/Messaging/Consumers/OrderInventoryReservedConsumer`
- `ProcessPaymentCommand`
  - Produced by: `Ordering.API/Saga/OrderStateMachine`
  - Consumed by: `Payment.Infrastructure/Messaging/ProcessPaymentConsumer`, `Ordering.Infrastructure/Messaging/Consumers/OrderPaymentProcessingConsumer`
- `PaymentCompletedEvent`
  - Produced by: `Payment.Infrastructure/Messaging/ProcessPaymentConsumer`
  - Consumed by: `Ordering.API/Saga/OrderStateMachine`
- `PaymentFailedEvent`
  - Produced by: `Payment.Infrastructure/Messaging/ProcessPaymentConsumer`
  - Consumed by: `Ordering.API/Saga/OrderStateMachine`
- `OrderCompletedEvent`
  - Produced by: `Ordering.API/Saga/OrderStateMachine`
  - Consumed by: `Notification.Worker/Consumers/OrderCompletedConsumer`, `Ordering.Infrastructure/Messaging/Consumers/OrderCompletedProjectionConsumer`
- `OrderCancelledEvent`
  - Produced by: `Ordering.API/Saga/OrderStateMachine`
  - Consumed by: `Notification.Worker/Consumers/OrderCancelledConsumer`, `Ordering.Infrastructure/Messaging/Consumers/OrderCancelledProjectionConsumer`
- `OrderStatusChangedEvent`
  - Produced by: `Ordering.Application/Commands/UpdateOrderStatusHandler` and ordering projection consumers
  - Consumed by: `Notification.Worker/Consumers/OrderStatusChangedConsumer`

## Findings

### Fixed

1. `Cart.API` dropped shipping address on checkout.
   - Frontend sent address payload to `/api/cart/checkout`.
   - Endpoint ignored request body and constructed `CheckoutCartCommand(buyerId)` with null address fields.
   - Result: `OrderSubmittedEvent` and persisted `Order` often had no shipping address.

2. SignalR buyer targeting was broken in real browsers.
   - Frontend used WebSockets with custom header `x-buyer-id`.
   - Browser WebSocket handshake does not reliably carry arbitrary headers from SignalR JS.
   - `BuyerIdUserIdProvider` and `NotificationHub` depended on that header, so `Clients.User(buyerId)` had no stable user mapping.

3. SignalR lifecycle only worked on initial app boot.
   - `NotificationService.start()` was triggered from app initializer only.
   - After login/register in an already-open SPA session, no hub connection was started.

4. `Order` read model drifted from saga state.
   - Saga moved through `ReservingInventory -> ProcessingPayment -> Completed/Cancelled`.
   - Persisted `Order` entity was only created on `OrderSubmittedEvent`.
   - `GET /api/orders/*` could stay at `Submitted` even when saga/payment had already advanced.

5. Failed payments were not persisted.
   - `PaymentFailedEvent` was published, but `PaymentTransaction` was only written on successful gateway responses.
   - Result: `/api/payments/order/{id}` returned no failed transaction record.

### Residual Gaps

1. Seller order correlation is still weak.
   - Seller order queries filter by `OrderItem.SellerId`.
   - Current cart/product snapshot flow does not reliably propagate seller/store identity into checkout-created `OrderItem`s.
   - Impact: seller dashboard order list can still be incomplete depending on how the order was created.

2. Manual order cancellation is not yet saga-aware.
   - `CancelOrderHandler` updates the order aggregate directly.
   - It still does not coordinate with saga compensation or payment rollback.
   - Impact: buyer-side cancel remains a partial flow and should not be treated as fully compensated orchestration.

## Applied Fixes

- `src/Microservices/Cart/Cart.API/Endpoints/CartEndpoints.cs`
  - Added `CheckoutRequest` body binding and forwarded address fields into `CheckoutCartCommand`.
- `src/web/src/app/app.config.ts`
  - Removed direct SignalR start from initializer; auth initialization remains sequential.
- `src/web/src/app/core/auth/auth.store.ts`
  - Start SignalR after `login`, `register`, and successful `checkAuth`.
  - Stop SignalR on `logout` and auth failure.
- `src/web/src/app/core/signalr/notification.service.ts`
  - Switched buyer identity transport from custom header to query string.
  - Guarded against anonymous/no-op starts.
- `src/Microservices/Notification/Notification.Worker/Hubs/UserIdProvider.cs`
  - Resolve user id from `buyerId` query string first, header second.
- `src/Microservices/Notification/Notification.Worker/Hubs/NotificationHub.cs`
  - Log handshake buyer identity from query string/header fallback.
- `src/Microservices/Ordering/Ordering.Infrastructure/Messaging/Consumers/*`
  - Added projection consumers to keep persisted `Order` in sync with inventory reservation, payment start, completion, and cancellation events.
- `src/Microservices/Ordering/Ordering.API/Program.cs`
  - Registered new ordering projection consumers.
- `src/Microservices/Payment/Payment.Application/Commands/ProcessPayment/*`
  - Internal payment persistence now records both success and failure outcomes.
- `src/Microservices/Payment/Payment.Infrastructure/Messaging/ProcessPaymentConsumer.cs`
  - Persist failed payment transactions before publishing `PaymentFailedEvent`.

## Verification

- Build/tests run after changes: pending
