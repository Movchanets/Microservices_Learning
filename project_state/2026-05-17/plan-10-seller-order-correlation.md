# Plan 10: Seller Order Correlation

## Goal
Propagate `SellerId` from product through cart to order items, so seller dashboard shows complete order list.

## Context
- **Current state:** `CartItem` and `OrderItemContract` have Sku, Quantity, Price — no SellerId. Seller dashboard queries `GET /api/orders/seller/{sellerId}` which filters by `OrderItem.SellerId`, but that field is always null.
- **Target state:** Every `OrderItem` has a valid `SellerId`. Seller dashboard shows all orders containing the seller's products.
- **Root cause:** SellerId never propagates from Catalog product → Cart item → Order item.

## Prerequisites
- Catalog product has `StoreId` — exists
- Store has `SellerId` — exists
- Cart accepts items with Sku, Quantity, Price — exists
- Ordering saga creates Order from `OrderSubmittedEvent` — exists

## Backend Changes

### 1. Add SellerId to Cart Domain
**File:** `src/Microservices/Cart/Cart.Domain/Aggregates/CartItem.cs`

Add `string? SellerId` property to `CartItem`.

### 2. Update Cart DTOs and Commands
**File:** `src/BuildingBlocks/SharedContracts/Dtos/CartItemDto.cs`

```csharp
public record CartItemDto(string Sku, int Quantity, decimal Price, string? SellerId = null);
```

**File:** `src/Microservices/Cart/Cart.Application/Commands/AddCartItemCommand.cs`

```csharp
public record AddCartItemCommand(string BuyerId, string Sku, int Quantity, string? SellerId = null)
    : IRequest<Result<CartDto>>;
```

### 3. Update OrderItemContract
**File:** `src/BuildingBlocks/SharedContracts/Dtos/OrderItemContract.cs`

```csharp
public record OrderItemContract(string Sku, int Quantity, decimal Price, string? SellerId = null);
```

### 4. Update Order Aggregate
**File:** `src/Microservices/Ordering/Ordering.Domain/Aggregates/Order.cs`

```csharp
public OrderItem AddItem(string sku, string name, decimal price, int quantity, string? sellerId = null)
```

### 5. Update OrderSubmittedConsumer
**File:** `src/Microservices/Ordering/Ordering.Infrastructure/Messaging/Consumers/OrderSubmittedConsumer.cs`

Pass `item.SellerId` when calling `order.AddItem()`.

### 6. Update Frontend Cart Service
**File:** `src/web/src/app/features/cart/cart.service.ts`

Include `sellerId` in cart item requests. Resolve from product data (CatalogStore).

### 7. Update Catalog Product → Cart Flow
**File:** `src/web/src/app/features/catalog/components/buy-box/buy-box.ts`

When adding to cart, include the product's `storeId` as `sellerId`.

## E2E Verification

### Spec File: `tests/E2ETests/tests/seller-order-correlation.spec.ts`

**Scenario:** Buyer purchases a product. Seller sees the order in their dashboard.

```
TEST: seller-order-correlation.spec.ts

Setup:
  1. Register buyer via API
  2. Login as existing seller
  3. Create store (seller) + verify (admin)
  4. Create product in store (seller)

Test: "buyer checkout creates order visible to seller"
  5. Login as buyer in browser
  6. Navigate to product detail page
  7. Click "Add to Cart" (buy-box component)
  8. Navigate to cart → verify item present
  9. Proceed to checkout → fill address → place order
  10. Wait for order completion (polling)
  11. Logout buyer

  12. Login as seller in browser
  13. Navigate to /seller → Orders tab
  14. Verify order appears in seller order list
  15. Verify order contains the correct product SKU
  16. Verify order status is "Completed"
```

### New Page Objects
- None needed — uses existing `SellerOrdersPage`

### New Fixtures
- Extend `checkout.fixture.ts` with `buyerApi` fixture

### Files to Create/Modify
```
tests/E2ETests/tests/seller-order-correlation.spec.ts     # NEW
tests/E2ETests/fixtures/checkout.fixture.ts               # Add buyerApi fixture
```

## Acceptance Criteria
- [ ] `CartItem` has `SellerId` property
- [ ] `OrderItemContract` includes `SellerId`
- [ ] `Order.AddItem()` accepts and stores `SellerId`
- [ ] `OrderSubmittedConsumer` passes `SellerId` from event to order
- [ ] Frontend sends `sellerId` when adding to cart
- [ ] `GET /api/orders/seller/{sellerId}` returns orders with matching items
- [ ] E2E test passes: buyer checkout → seller sees order
- [ ] All existing tests still pass (218 unit, 45 contract, 30 integration, 293 frontend)

## Verification Commands
```bash
dotnet build Marketplace.slnx
dotnet test tests/UnitTests/ --no-build
dotnet test tests/ContractTests/ --no-build
npx ng test --watch=false
npx playwright test tests/E2ETests/tests/seller-order-correlation.spec.ts
```
