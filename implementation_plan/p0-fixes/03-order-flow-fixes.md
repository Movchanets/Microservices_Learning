# P0-03 — Order Flow Fixes

**Goal**: Fix order item price resolution (currently hardcoded to 0), add address collection, and add order cancellation.

**Fixes**: MISSING.md #2.1, #2.2, #2.5

---

## Step 1: Fix price resolution in OrderSubmittedConsumer

File: `src/Microservices/Ordering/Ordering.Infrastructure/Messaging/Consumers/OrderSubmittedConsumer.cs`

**Problem**: Line 37 hardcodes `0m` for item prices:
```csharp
order.AddItem(item.Sku, item.Sku, 0m, item.Quantity);
```

**Fix**: The `OrderSubmittedEvent` from Cart needs to include price data. Two options:

**Option A (Preferred)**: Cart includes prices in the event.
- Cart domain: `CartItem` needs a `Price` property
- Cart checkout: Include price in `OrderItemContract`
- Update `SharedContracts/Dtos/OrderItemContract.cs` to include `decimal Price`
- Update `Cart.Domain/Aggregates/ShoppingCart.cs` to store prices
- Cart needs to fetch prices from Catalog at checkout time (or store them when items are added)

**Option B (Simpler, less correct)**: Ordering resolves prices from Catalog.
- Ordering consumer calls Catalog API to get current prices
- Problem: price may change between cart-add and checkout

**Recommended**: Option A — Cart stores prices when items are added, passes them through to the event.

### Changes needed:
1. `Cart.Domain/Aggregates/CartItem.cs` — add `decimal Price` property
2. `Cart.Application/Commands/UpdateCartCommand.cs` — accept price in `CartItemDto`
3. `SharedContracts/Dtos/OrderItemContract.cs` — add `decimal Price`
4. `Cart.Application/Commands/CheckoutCartCommand.cs` — include price in event items
5. `Ordering.Infrastructure/Messaging/Consumers/OrderSubmittedConsumer.cs` — use `item.Price`

## Step 2: Add address to checkout flow

### Backend — Update OrderSubmittedEvent

File: `src/BuildingBlocks/SharedContracts/Events/Cart/OrderSubmittedEvent.cs`

Add address fields:
```csharp
public record OrderSubmittedEvent(
    Guid CorrelationId,
    string BuyerId,
    List<OrderItemContract> Items,
    string ShippingAddress,
    string ShippingCity,
    string ShippingPostalCode,
    string ShippingCountry,
    DateTime Timestamp) : CorrelatedBy<Guid>;
```

### Backend — Update Order aggregate

File: `src/Microservices/Ordering/Ordering.Domain/Aggregates/Order.cs`

The `Order.Create()` method and `Address` value object already exist. Update `OrderSubmittedConsumer` to pass address:
```csharp
var address = new Address(
    evt.ShippingAddress,
    evt.ShippingCity,
    evt.ShippingPostalCode,
    evt.ShippingCountry);
var order = Order.Create(evt.BuyerId, address);
```

### Frontend — Add address form to checkout

File: `src/web/src/app/features/checkout/checkout-page/checkout-page.ts`

Add address form fields (street, city, postal code, country) using Angular reactive forms or signals. Submit address with the checkout request.

### Frontend — Update checkout models

File: `src/web/src/app/features/checkout/checkout.models.ts`

Add:
```typescript
export interface ShippingAddress {
  street: string;
  city: string;
  postalCode: string;
  country: string;
}
```

## Step 3: Add order cancellation endpoint

### Backend

File: `src/Microservices/Ordering/Ordering.API/Endpoints/OrderEndpoints.cs`

Add cancellation endpoint:
```csharp
group.MapPost("/{id:guid}/cancel", async (
    Guid id,
    ISender sender,
    CancellationToken ct) =>
{
    var result = await sender.Send(new CancelOrderCommand(id), ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Error);
})
.WithName("CancelOrder")
.RequireAuthorization()
.Produces(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest);
```

The `CancelOrderCommand` and handler already exist at:
- `Ordering.Application/Commands/CancelOrder/CancelOrderCommand.cs`
- `Ordering.Application/Commands/CancelOrder/CancelOrderHandler.cs`

Verify the handler publishes a cancellation event that the saga can consume to release inventory.

### Frontend — Add cancel button

File: `src/web/src/app/features/orders/order-detail/order-detail.ts`

Add a "Cancel Order" button for orders with status `Submitted` or `Reserved`:
```typescript
async onCancel(): Promise<void> {
  if (confirm('Cancel this order?')) {
    await this.orderStore.cancelOrder(this.orderId());
  }
}
```

## Verification
- `dotnet build Marketplace.slnx`
- `pnpm nx run web:build`
- Create order with non-zero prices
- Create order with address
- Cancel a pending order

## Done When
- [ ] Order items have real prices (not 0m)
- [ ] Checkout collects shipping address
- [ ] Order cancellation endpoint works
- [ ] Frontend cancel button on order detail
- [ ] All builds pass
