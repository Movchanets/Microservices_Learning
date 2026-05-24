# Plan 03: Cart & Checkout Optimization — ✅ COMPLETE (2026-05-16)

## Goal
Add slide-out cart drawer (mini-cart), single-item cart endpoints, address form in checkout, and improve the overall cart/checkout UX.

## Context
- **Current state:** Cart page with full replacement API. No slide-out drawer. No address form. No single-item endpoints.
- **Target state:** Slide-out drawer on add-to-cart, single-page accordion checkout with address, shipping method, payment sections.
- **Design ref:** `plans/future_design/cart_and_checkout.md`
- **Backend gaps:** No single-item cart endpoints (MISSING.md #6.10), no address in checkout (#2.2, #5.7)

## Prerequisites
- Cart.API has POST /api/cart (full replacement) — exists
- CartStore has addToCart, removeFromCart, updateQuantity — exists
- CheckoutStore has submitCheckout — exists

## Backend Changes

### 1. Add Single-Item Cart Endpoints
**File:** `src/Microservices/Cart/Cart.API/Endpoints/CartEndpoints.cs`

```csharp
// Add single item
group.MapPost("/items", async (
    ClaimsPrincipal user,
    [FromBody] AddCartItemRequest request,
    [FromServices] ISender sender,
    CancellationToken ct) =>
{
    var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var result = await sender.Send(new AddCartItemCommand(buyerId!, request.Sku, request.Quantity, request.Price), ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});

// Update item quantity
group.MapPut("/items/{sku}", async (
    ClaimsPrincipal user,
    string sku,
    [FromBody] UpdateCartItemRequest request,
    [FromServices] ISender sender,
    CancellationToken ct) =>
{
    var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var result = await sender.Send(new UpdateCartItemCommand(buyerId!, sku, request.Quantity), ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});

// Remove single item
group.MapDelete("/items/{sku}", async (
    ClaimsPrincipal user,
    string sku,
    [FromServices] ISender sender,
    CancellationToken ct) =>
{
    var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var result = await sender.Send(new RemoveCartItemCommand(buyerId!, sku), ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});
```

**New files:**
- `Cart.Application/Commands/AddCartItemCommand.cs` + Handler
- `Cart.Application/Commands/UpdateCartItemCommand.cs` + Handler
- `Cart.Application/Commands/RemoveCartItemCommand.cs` + Handler
- `Cart.Application/Commands/AddCartItemValidator.cs`

These commands should use `ShoppingCart.AddItem()`, `ShoppingCart.UpdateItemQuantity()`, `ShoppingCart.RemoveItem()` domain methods.

### 2. Add Address to Order Flow
**File:** `src/Microservices/Ordering/Ordering.Application/Commands/CreateOrder/CreateOrderCommand.cs`

Add address fields to the command:
```csharp
public sealed record CreateOrderCommand(
    string BuyerId,
    List<CreateOrderItemDto> Items,
    string? ShippingAddressLine1,
    string? ShippingAddressLine2,
    string? ShippingCity,
    string? ShippingState,
    string? ShippingPostalCode,
    string? ShippingCountry) : IRequest<Result<Guid>>;
```

Update Order aggregate to store address (it already has Address value object).

### 3. Update Cart Checkout Command
**File:** `src/Microservices/Cart/Cart.Application/Commands/CheckoutCart/CheckoutCartCommand.cs`

Pass address data through to OrderSubmittedEvent:
```csharp
public sealed record CheckoutCartCommand(
    string BuyerId,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country) : IRequest<Result<CheckoutResult>>;
```

## Frontend Changes

### 4. Create Slide-Out Cart Drawer
**New file:** `src/web/src/app/shared/components/cart-drawer/cart-drawer.ts`

- Slides in from the right when triggered
- Shows cart items with quantity controls
- Shows total price and item count
- "Free Shipping Progress Bar" (e.g., "Add $15 more to unlock free shipping!")
- "Go to Cart" and "Checkout" buttons
- Close on click outside or Escape key
- Triggered by CartStore.addToCart() success

### 5. Update CartStore for Single-Item Operations
**File:** `src/web/src/app/features/cart/cart.store.ts`

Replace full-replacement methods with single-item API calls:
```typescript
async addToCart(sku: string, quantity: number, price: number): Promise<void> {
  // Call POST /api/cart/items instead of full replacement
  const cart = await cartService.addItem(sku, quantity, price);
  patchState(store, { items: cart.items });
  // Show cart drawer
  this.showDrawer();
}
```

### 6. Update CartService
**File:** `src/web/src/app/features/cart/cart.service.ts`

Add methods:
```typescript
addItem(sku: string, quantity: number, price: number): Promise<ShoppingCart> {
  return firstValueFrom(this.http.post<ShoppingCart>('/api/cart/items', { sku, quantity, price }));
}

updateItem(sku: string, quantity: number): Promise<ShoppingCart> {
  return firstValueFrom(this.http.put<ShoppingCart>(`/api/cart/items/${sku}`, { quantity }));
}

removeItem(sku: string): Promise<ShoppingCart> {
  return firstValueFrom(this.http.delete<ShoppingCart>(`/api/cart/items/${sku}`));
}
```

### 7. Create Address Form Component
**New file:** `src/web/src/app/features/checkout/address-form/address-form.ts`

- Reactive form with: addressLine1, addressLine2, city, state, postalCode, country
- Validation: required fields, postal code format
- Country dropdown (ISO codes)
- Save to localStorage for future orders

### 8. Refactor Checkout Page to Accordion Style
**File:** `src/web/src/app/features/checkout/checkout-page/checkout-page.ts`

Single page with expandable sections:
1. **Shipping Address** (expanded by default) — AddressFormComponent
2. **Shipping Method** (collapsed) — Radio buttons (Standard, Express)
3. **Order Summary** (collapsed) — Cart items + totals
4. **Payment** (collapsed) — Confirm button (simulated for now)

Each section shows a checkmark when completed. Sections expand/collapse on click.

### 9. Update CheckoutStore
**File:** `src/web/src/app/features/checkout/checkout.store.ts`

Add address state:
```typescript
interface CheckoutState {
  address: Address | null;
  shippingMethod: 'standard' | 'express';
  submitting: boolean;
  error: string | null;
  order: Order | null;
}
```

### 10. Update Header to Show Cart Counter
**File:** `src/web/src/app/shared/components/header/header.ts`

Add cart icon with badge showing `CartStore.totalItems()` signal. Click opens cart drawer.

## Files to Modify/Create

| Action | File |
|--------|------|
| CREATE | `Cart.Application/Commands/AddCartItemCommand.cs` + Handler |
| CREATE | `Cart.Application/Commands/UpdateCartItemCommand.cs` + Handler |
| CREATE | `Cart.Application/Commands/RemoveCartItemCommand.cs` + Handler |
| MODIFY | `Cart.API/Endpoints/CartEndpoints.cs` |
| MODIFY | `Ordering.Application/Commands/CreateOrder/CreateOrderCommand.cs` |
| MODIFY | `Cart.Application/Commands/CheckoutCart/CheckoutCartCommand.cs` |
| CREATE | `src/web/src/app/shared/components/cart-drawer/cart-drawer.ts` |
| MODIFY | `src/web/src/app/features/cart/cart.store.ts` |
| MODIFY | `src/web/src/app/features/cart/cart.service.ts` |
| MODIFY | `src/web/src/app/features/cart/cart.models.ts` |
| CREATE | `src/web/src/app/features/checkout/address-form/address-form.ts` |
| MODIFY | `src/web/src/app/features/checkout/checkout-page/checkout-page.ts` |
| MODIFY | `src/web/src/app/features/checkout/checkout.store.ts` |
| MODIFY | `src/web/src/app/features/checkout/checkout.models.ts` |
| MODIFY | `src/web/src/app/shared/components/header/header.ts` |

## Verification
1. `dotnet build Marketplace.slnx` — no errors
2. `ng build` — no errors
3. `dotnet test tests/UnitTests/Cart.UnitTests/` — passes
4. Manual: Add item → slide-out drawer appears
5. Manual: Drawer shows items, total, free shipping progress
6. Manual: Single-item add/remove/update works
7. Manual: Checkout → address form → accordion sections
8. Manual: Order created with address data
