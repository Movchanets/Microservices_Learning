# Plan 09: Order Cancellation & Status Management

## Goal
Add order cancellation endpoint for buyers and order status update endpoint for sellers. Add corresponding UI.

## Context
- **Current state:** CancelOrderCommand exists in Ordering.Application but no API endpoint. No order status update endpoint. Buyers can't cancel. Sellers can't mark as shipped/completed.
- **Target state:** Buyers can cancel pending orders. Sellers can update order status (Processing → Shipped → Delivered). Real-time notifications via SignalR.
- **Backend gaps:** MISSING.md #6.8 (cancel endpoint), #6.9 (status update endpoint)

## Prerequisites
- Ordering.API has GET /api/orders/{id} — exists
- Ordering.Domain has Order aggregate with Cancel() method — exists
- CancelOrderCommand exists in Ordering.Application — exists
- SignalR notifications working — exists

## Backend Changes

### 1. Add Cancel Order Endpoint
**File:** `src/Microservices/Ordering/Ordering.API/Endpoints/OrderEndpoints.cs`

```csharp
group.MapPost("/{id:guid}/cancel", async (
    Guid id,
    ClaimsPrincipal user,
    [FromServices] ISender sender,
    CancellationToken ct) =>
{
    var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var result = await sender.Send(new CancelOrderCommand(id, buyerId!), ct);
    return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
});
```

### 2. Add Update Order Status Endpoint
**File:** `src/Microservices/Ordering/Ordering.API/Endpoints/OrderEndpoints.cs`

```csharp
group.MapPut("/{id:guid}/status", async (
    Guid id,
    [FromBody] UpdateOrderStatusRequest request,
    [FromServices] ISender sender,
    CancellationToken ct) =>
{
    var result = await sender.Send(new UpdateOrderStatusCommand(id, request.Status, request.Notes), ct);
    return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
})
.RequireAuthorization("Seller");
```

**New files:**
- `Ordering.Application/Commands/UpdateOrderStatus/UpdateOrderStatusCommand.cs` + Handler + Validator
- `Ordering.Application/Commands/UpdateOrderStatus/UpdateOrderStatusRequest.cs`

### 3. Update Order Aggregate for Status Transitions
**File:** `src/Microservices/Ordering/Ordering.Domain/Aggregates/Order.cs`

Ensure proper status transitions:
```csharp
public void UpdateStatus(OrderStatus newStatus, string? notes = null)
{
    Status = newStatus switch
    {
        OrderStatus.Processing when Status == OrderStatus.Created => OrderStatus.Processing,
        OrderStatus.Shipped when Status == OrderStatus.Processing => OrderStatus.Shipped,
        OrderStatus.Delivered when Status == OrderStatus.Shipped => OrderStatus.Delivered,
        _ => throw new DomainException($"Invalid status transition from {Status} to {newStatus}")
    };
    UpdatedAt = DateTime.UtcNow;
}
```

### 4. Add Order Status Notification
**File:** `src/Microservices/Ordering/Ordering.Application/Commands/UpdateOrderStatus/UpdateOrderStatusHandler.cs`

After updating status, publish OrderStatusChangedEvent for SignalR notification:
```csharp
await _publishEndpoint.Publish(new OrderStatusChangedEvent(
    order.Id, order.BuyerId, newStatus, DateTime.UtcNow));
```

**New file:** `BuildingBlocks.SharedContracts/Events/OrderStatusChangedEvent.cs`

### 5. Update Notification Worker
**File:** `src/Microservices/Notification/Notification.Worker/Consumers/OrderStatusChangedConsumer.cs`

Consume OrderStatusChangedEvent and push to SignalR:
```csharp
public class OrderStatusChangedConsumer : IConsumer<OrderStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedEvent> context)
    {
        var message = context.Message;
        await _hubContext.Clients.User(message.BuyerId)
            .SendAsync("OrderUpdate", new
            {
                OrderId = message.OrderId,
                Status = message.Status.ToString(),
                Timestamp = message.Timestamp
            });
    }
}
```

## Frontend Changes

### 6. Add Cancel Order to OrderService
**File:** `src/web/src/app/features/orders/order.service.ts`

```typescript
cancelOrder(orderId: string): Promise<void> {
  return firstValueFrom(this.http.post<void>(`/api/orders/${orderId}/cancel`, {}));
}
```

### 7. Add Cancel Order to OrderStore
**File:** `src/web/src/app/features/orders/order.store.ts`

```typescript
async cancelOrder(orderId: string): Promise<void> {
  patchState(store, { loading: true });
  await orderService.cancelOrder(orderId);
  // Update local state
  const orders = store.orders().map(o =>
    o.id === orderId ? { ...o, status: 'Cancelled' } : o
  );
  patchState(store, { orders, loading: false });
}
```

### 8. Add Cancel Button to Order Detail
**File:** `src/web/src/app/features/orders/order-detail/order-detail.ts`

Show "Cancel Order" button when:
- Order status is Created or Processing
- User is the buyer (order.buyerId === authStore.user().id)

Button opens confirmation dialog, then calls OrderStore.cancelOrder().

### 9. Create Order Status Badge Component
**New file:** `src/web/src/app/features/orders/components/status-badge/status-badge.ts`

Color-coded badges:
- Created → Gray
- Processing → Blue
- Shipped → Purple
- Delivered → Green
- Cancelled → Red
- Faulted → Orange

### 10. Add Seller Order Status Update
**File:** `src/web/src/app/features/seller-dashboard/seller-orders/seller-orders.ts`

Add status dropdown for each order:
- Current status shown as badge
- Dropdown with valid next statuses
- "Update" button calls API

### 11. Create Seller Order Detail Component
**New file:** `src/web/src/app/features/seller-dashboard/seller-order-detail/seller-order-detail.ts`

Full order view for sellers:
- Order items list
- Buyer info (name, email)
- Shipping address
- Status timeline
- Status update controls
- Notes field for status changes

### 12. Update Seller Routes
**File:** `src/web/src/app/features/seller-dashboard/seller.routes.ts`

```typescript
{ path: 'orders/:id', loadComponent: () => import('./seller-order-detail/seller-order-detail').then(m => m.SellerOrderDetailComponent) },
```

### 13. Update SignalR for Status Updates
**File:** `src/web/src/app/core/signalr/notification.service.ts`

Already handles OrderUpdate messages. Ensure the notification bridge updates OrderStore when status changes.

## Files to Modify/Create

| Action | File |
|--------|------|
| MODIFY | `Ordering.API/Endpoints/OrderEndpoints.cs` |
| CREATE | `Ordering.Application/Commands/UpdateOrderStatus/` (Command, Handler, Validator, Request) |
| MODIFY | `Ordering.Domain/Aggregates/Order.cs` |
| CREATE | `BuildingBlocks.SharedContracts/Events/OrderStatusChangedEvent.cs` |
| CREATE | `Notification.Worker/Consumers/OrderStatusChangedConsumer.cs` |
| MODIFY | `src/web/src/app/features/orders/order.service.ts` |
| MODIFY | `src/web/src/app/features/orders/order.store.ts` |
| MODIFY | `src/web/src/app/features/orders/order-detail/order-detail.ts` |
| CREATE | `src/web/src/app/features/orders/components/status-badge/status-badge.ts` |
| MODIFY | `src/web/src/app/features/seller-dashboard/seller-orders/seller-orders.ts` |
| CREATE | `src/web/src/app/features/seller-dashboard/seller-order-detail/seller-order-detail.ts` |
| MODIFY | `src/web/src/app/features/seller-dashboard/seller.routes.ts` |

## Verification
1. `dotnet build Marketplace.slnx` — no errors
2. `ng build` — no errors
3. `dotnet test tests/UnitTests/Ordering.UnitTests/` — passes
4. Manual: Buyer views order → "Cancel" button visible for pending orders
5. Manual: Cancel order → status changes to Cancelled
6. Manual: Seller views orders → status dropdown available
7. Manual: Update status → buyer receives SignalR notification
8. Manual: Invalid status transition → error message
