# Plan 11: Saga-Aware Order Cancellation

## Goal
Make `CancelOrderHandler` coordinate with the ordering saga so cancellation triggers inventory release and payment rollback.

## Context
- **Current state:** `CancelOrderHandler` calls `order.Cancel(reason)` directly on the aggregate. No integration event is published. Saga doesn't know about the cancellation. Inventory stays reserved. Payment is not rolled back.
- **Target state:** Cancellation publishes `CancelOrderCommand` to the saga. Saga transitions to `Cancelled` state, publishes `ReleaseInventoryCommand` and `RefundPaymentCommand`. Projection consumers update the persisted Order.
- **Root cause:** `CancelOrderHandler` was implemented as a direct aggregate update, bypassing the saga orchestration.

## Prerequisites
- `CancelOrderCommand` and `CancelOrderHandler` exist — `Ordering.Application/Commands/CancelOrder/`
- `OrderStateMachine` has `Cancelled` state — exists
- `OrderCancelledEvent` exists — exists
- `OrderCancelledProjectionConsumer` exists — exists

## Backend Changes

### 1. Add CancelOrderEvent to SharedContracts
**File:** `src/BuildingBlocks/SharedContracts/Events/Ordering/CancelOrderEvent.cs`

```csharp
public record CancelOrderEvent(
    Guid CorrelationId,
    Guid OrderId,
    string BuyerId,
    string? Reason,
    DateTime Timestamp) : CorrelatedBy<Guid>;
```

### 2. Update CancelOrderHandler to Publish Event
**File:** `src/Microservices/Ordering/Ordering.Application/Commands/CancelOrder/CancelOrderHandler.cs`

Instead of directly updating the aggregate, publish `CancelOrderEvent` via `IPublishEndpoint`:

```csharp
public sealed class CancelOrderHandler(
    IOrderRepository repository,
    IPublishEndpoint publishEndpoint,
    IUnitOfWork uow) : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await repository.GetByIdAsync(request.OrderId, ct);
        if (order is null) return Result<bool>.Failure("Order not found");

        // Validate: only Submitted/InventoryReserved/PaymentProcessing can be cancelled
        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Faulted)
            return Result<bool>.Failure($"Cannot cancel order in {order.Status} state");

        // Publish event to saga — saga handles compensation
        await publishEndpoint.Publish(new CancelOrderEvent(
            order.Id, order.Id, order.BuyerId, request.Reason, DateTime.UtcNow), ct);

        return Result<bool>.Success(true);
    }
}
```

### 3. Add CancelOrder Event to Saga
**File:** `src/Microservices/Ordering/Ordering.API/Saga/OrderStateMachine.cs`

```csharp
// New event
public Event<CancelOrderEvent> CancelOrder { get; private set; } = null!;

// Correlation
Event(() => CancelOrder, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

// During any active state, handle cancellation
During(ReservingInventory, ProcessingPayment,
    When(CancelOrder)
        .Then(ctx => { ctx.Saga.UpdatedAt = DateTime.UtcNow; })
        .Publish(ctx => new OrderCancelledEvent(
            ctx.Saga.CorrelationId, ctx.Saga.OrderId, ctx.Saga.BuyerId,
            ctx.Message.Reason ?? "Cancelled by buyer", DateTime.UtcNow))
        .TransitionTo(Cancelled));

// During Completed — reject (handled by handler validation)
```

### 4. Add Inventory Release to Saga Compensation
**File:** `src/Microservices/Ordering/Ordering.API/Saga/OrderStateMachine.cs`

When transitioning to `Cancelled` from `ReservingInventory`, publish `ReleaseInventoryCommand`:

```csharp
.Publish(ctx => new ReleaseInventoryCommand(
    ctx.Saga.CorrelationId, ctx.Saga.OrderId,
    JsonSerializer.Deserialize<List<OrderItemContract>>(ctx.Saga.ItemsJson)!))
```

### 5. Add ReleaseInventoryCommand to SharedContracts
**File:** `src/BuildingBlocks/SharedContracts/Commands/Inventory/ReleaseInventoryCommand.cs`

```csharp
public record ReleaseInventoryCommand(
    Guid CorrelationId, Guid OrderId, List<OrderItemContract> Items);
```

### 6. Add ReleaseInventoryConsumer to Inventory
**File:** `src/Microservices/Inventory/Inventory.Infrastructure/Messaging/Consumers/ReleaseInventoryConsumer.cs`

```csharp
public sealed class ReleaseInventoryConsumer(
    IInventoryRepository repository, IUnitOfWork uow,
    ILogger<ReleaseInventoryConsumer> logger) : IConsumer<ReleaseInventoryCommand>
{
    public async Task Consume(ConsumeContext<ReleaseInventoryCommand> context)
    {
        foreach (var item in context.Message.Items)
        {
            var inventory = await repository.GetBySkuAsync(item.Sku, context.CancellationToken);
            if (inventory is not null)
            {
                inventory.Release(item.Quantity);
                repository.Update(inventory);
            }
        }
        await uow.SaveChangesAsync(context.CancellationToken);
    }
}
```

### 7. Register ReleaseInventoryConsumer in Inventory.API
**File:** `src/Microservices/Inventory/Inventory.API/Program.cs`

Add consumer registration.

## E2E Verification

### Spec File: `tests/E2ETests/tests/saga-aware-cancellation.spec.ts`

**Scenario:** Buyer places order, then cancels. Inventory is released. Order status reflects cancellation.

```
TEST: saga-aware-cancellation.spec.ts

Setup:
  1. Register buyer via API
  2. Login as seller, create store, verify, create product (via API)
  3. Note initial inventory level for product SKU

Test: "buyer cancels order and inventory is released"
  4. Login as buyer in browser
  5. Add product to cart via API
  6. Navigate to /cart → proceed to checkout
  7. Fill address → place order
  8. Wait for order completion (status = Completed)
  9. Navigate to /orders → click order detail
  10. Verify "Cancel Order" button is NOT visible (completed orders can't be cancelled)

Test: "buyer cancels pending order triggers saga compensation"
  11. Create a NEW product with controlled inventory
  12. Add to cart → checkout → place order
  13. QUICKLY navigate to order detail (before completion)
  14. If cancel button visible: click "Cancel Order"
  15. Confirm cancellation dialog
  16. Wait for order status to change to "Cancelled"
  17. Verify via API: inventory for SKU is restored

Test: "cancelled order shows correct status in buyer orders"
  18. Navigate to /orders
  19. Verify cancelled order shows "Cancelled" status badge
  20. Click order detail → verify timeline shows cancellation
```

### New Page Objects
- None — uses existing `OrderDetailEnhancedPage`

### Files to Create/Modify
```
tests/E2ETests/tests/saga-aware-cancellation.spec.ts     # NEW
```

## Acceptance Criteria
- [ ] `CancelOrderEvent` exists in SharedContracts
- [ ] `CancelOrderHandler` publishes event (doesn't directly update aggregate)
- [ ] `OrderStateMachine` handles `CancelOrder` in ReservingInventory and ProcessingPayment states
- [ ] Saga publishes `ReleaseInventoryCommand` when cancelling from ReservingInventory
- [ ] `ReleaseInventoryConsumer` restores inventory quantities
- [ ] `OrderCancelledProjectionConsumer` updates persisted Order status
- [ ] E2E test passes: buyer cancels → order status = Cancelled → inventory restored
- [ ] All existing tests still pass

## Verification Commands
```bash
dotnet build Marketplace.slnx
dotnet test tests/UnitTests/Ordering.UnitTests/ --no-build
dotnet test tests/ContractTests/ --no-build
dotnet test tests/IntegrationTests/Inventory.IntegrationTests/ --no-build
npx playwright test tests/E2ETests/tests/saga-aware-cancellation.spec.ts
```
