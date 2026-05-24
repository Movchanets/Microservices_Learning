# P1-01 — Seller Order Management

**Goal**: Let sellers view orders containing their products.

**Fixes**: MISSING.md #5.1, #6.1

**Depends on**: P0-01 (Auth)

---

## Backend

### Add seller order query

File: `src/Microservices/Ordering/Ordering.Application/Queries/ListOrdersBySeller/ListOrdersBySellerQuery.cs`
```csharp
public sealed record ListOrdersBySellerQuery(string SellerId) : IRequest<Result<IReadOnlyList<OrderDto>>>;
```

Handler: Join OrderItems with Catalog products by SKU, filter by sellerId.

### Add endpoint

File: `src/Microservices/Ordering/Ordering.API/Endpoints/OrderEndpoints.cs`
```csharp
group.MapGet("/seller/{sellerId}", async (
    string sellerId,
    ISender sender,
    CancellationToken ct) =>
{
    var result = await sender.Send(new ListOrdersBySellerQuery(sellerId), ct);
    return Results.Ok(result.Value);
})
.WithName("GetOrdersBySeller")
.RequireAuthorization("Seller");
```

## Frontend

### Add seller orders page

File: `src/web/src/app/features/seller-dashboard/seller-orders/seller-orders.ts`

Table showing orders containing the seller's products, with status, date, total.

### Add route

File: `src/web/src/app/features/seller-dashboard/seller.routes.ts`
```typescript
{
  path: 'orders',
  loadComponent: () => import('./seller-orders/seller-orders').then(c => c.SellerOrdersComponent),
}
```

## Done When
- [ ] `GET /api/orders/seller/{sellerId}` endpoint works
- [ ] Seller orders page shows filtered orders
- [ ] Route guarded by Seller role
