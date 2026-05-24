# Test Plan: Cart Service

## Current Coverage

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Unit | ShoppingCartTests, AnonymousShoppingCartTests, AddCartItemCommandTests, AnonymousCartCommandTests, UpdateCartCommandHandlerTests, CheckoutCartCommandHandlerTests, GetCartQueryHandlerTests | ~35 | Covered |
| Integration | CartRepositoryTests, CatalogEventConsumerTests | ~10 | Partially Covered |
| Contract | CatalogToCartContractTests | ~5 | Covered |
| E2E | — | 0 | Not Covered |

## Test Scenarios — Unit

- [x] ShoppingCart add item
- [x] ShoppingCart remove item
- [x] ShoppingCart update quantity
- [x] ShoppingCart total calculation
- [x] AnonymousShoppingCart creation
- [x] AnonymousShoppingCart merge with authenticated
- [x] AddCartItemCommand handler
- [x] UpdateCartCommand handler
- [x] CheckoutCartCommand handler
- [x] GetCartQuery handler
- [ ] Cart expiration (TTL)
- [ ] Cart with out-of-stock item at checkout

## Test Scenarios — Integration

- [x] CartRepository persists and retrieves
- [x] CatalogEventConsumer handles ProductUpdated
- [ ] Cart merge on login (anonymous → authenticated)
- [ ] Cart Redis TTL expiration
- [ ] Concurrent cart updates (race condition)

## Test Scenarios — E2E

- [ ] Add item from catalog page
- [ ] Add item from product detail page
- [ ] Remove item from cart
- [ ] Update item quantity
- [ ] Cart drawer open/close/empty state
- [ ] Anonymous cart (X-Cart-Id header)
- [ ] Cart merge on login
- [ ] Cart persistence across page refresh
- [ ] Out-of-stock error handling
- [ ] Cart empty after successful order

## Gaps & Priority

| Gap | Priority | Notes |
|-----|----------|-------|
| ALL E2E tests removed | P0 | 8 tests deleted — add-to-cart, cart-drawer both gone |
| Anonymous cart E2E | P0 | BuyerId nullable, X-Cart-Id header — critical revenue path |
| Cart merge on login E2E | P0 | Checkout requires auth + CartId pass-through |
| Cart expiration test | P2 | Redis TTL behavior untested |
