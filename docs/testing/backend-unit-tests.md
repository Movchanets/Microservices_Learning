# Backend Unit Test Inventory

**Project:** Marketplace Microservices
**Framework:** xUnit + Moq + FluentAssertions
**Last Updated:** 2026-05-26
**Total:** 57 test files, 239 tests

---

## Test Projects

| Project | Path | Test Files | ~Tests |
|---------|------|-----------|--------|
| Cart.UnitTests | `tests/UnitTests/Cart.UnitTests/` | 7 | 35 |
| Catalog.UnitTests | `tests/UnitTests/Catalog.UnitTests/` | 7 | 45 |
| Identity.UnitTests | `tests/UnitTests/Identity.UnitTests/` | 7 | 40 |
| Inventory.UnitTests | `tests/UnitTests/Inventory.UnitTests/` | 3 | 18 |
| Notification.UnitTests | `tests/UnitTests/Notification.UnitTests/` | 4 | 15 |
| Ordering.UnitTests | `tests/UnitTests/Ordering.UnitTests/` | 6 | 35 |
| Payment.UnitTests | `tests/UnitTests/Payment.UnitTests/` | 5 | 25 |
| Search.UnitTests | `tests/UnitTests/Search.UnitTests/` | 3 | 12 |
| StoreManagement.UnitTests | `tests/UnitTests/StoreManagement.UnitTests/` | 6 | 25 |
| ApiGateway.UnitTests | `tests/UnitTests/ApiGateway.UnitTests/` | 3 | 3 |
| BuildingBlocks.Infrastructure.UnitTests | `tests/UnitTests/BuildingBlocks.Infrastructure.UnitTests/` | 2 | 4 |
| BuildingBlocks.SharedContracts.UnitTests | `tests/BuildingBlocks/BuildingBlocks.SharedContracts.UnitTests/` | 1 | 1 |

---

## Cart.UnitTests (7 files, ~35 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `Domain/ShoppingCartTests.cs` | Domain | Add/remove item, update quantity, total calculation |
| `Domain/AnonymousShoppingCartTests.cs` | Domain | Anonymous cart creation, merge with authenticated |
| `Application/AddCartItemCommandHandlerTests.cs` | Application | AddCartItemCommand handler |
| `Application/AnonymousCartCommandTests.cs` | Application | Anonymous cart command handling |
| `Application/UpdateCartCommandHandlerTests.cs` | Application | UpdateCartCommand handler |
| `Application/CheckoutCartCommandHandlerTests.cs` | Application | CheckoutCartCommand handler |
| `Application/GetCartQueryHandlerTests.cs` | Application | GetCartQuery handler |

**Gaps:** Cart expiration (TTL), out-of-stock item at checkout

---

## Catalog.UnitTests (7 files, ~45 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `Domain/ProductTests.cs` | Domain | Product entity behavior |
| `Domain/CategoryTests.cs` | Domain | Category entity behavior |
| `Application/CreateProductCommandHandlerTests.cs` | Application | CreateProductCommand handler |
| `Application/UpdateProductPriceCommandHandlerTests.cs` | Application | UpdateProductPriceCommand handler |
| `Application/DeleteProductCommandHandlerTests.cs` | Application | DeleteProductCommand handler |
| `Application/GetProductsQueryHandlerTests.cs` | Application | GetProductsQuery handler |
| `Application/GetProductRecommendationsHandlerTests.cs` | Application | GetProductRecommendations handler |
| `Application/GetCategoriesQueryHandlerTests.cs` | Application | GetCategoriesQuery handler |
| `BaseTest.cs` | — | Shared test base/fixtures |

**Gaps:** SKU CRUD handlers, product activation/deactivation

---

## Identity.UnitTests (7 files, ~40 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `Domain/UserTests.cs` | Domain | User entity behavior |
| `Application/LoginUserHandlerTests.cs` | Application | LoginUserCommand handler |
| `Application/RegisterUserHandlerTests.cs` | Application | RegisterUserCommand handler |
| `Application/RegisterUserValidatorTests.cs` | Application | Registration validation rules |
| `Application/GetUserByIdHandlerTests.cs` | Application | GetUserByIdQuery handler |
| `Application/ForgotPassword/ForgotPasswordHandlerTests.cs` | Application | ForgotPasswordCommand handler |
| `Infrastructure/PasswordHasherServiceTests.cs` | Infrastructure | Password hashing (BCrypt) |
| `Infrastructure/JwtTokenGeneratorTests.cs` | Infrastructure | JWT token generation |

**Gaps:** Token refresh, role management, password reset completion

---

## Inventory.UnitTests (3 files, ~18 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `Domain/InventoryItemTests.cs` | Domain | InventoryItem entity behavior |
| `Application/ReserveStockCommandHandlerTests.cs` | Application | ReserveStockCommand handler |
| `Application/ReleaseStockCommandHandlerTests.cs` | Application | ReleaseStockCommand handler |

**Gaps:** Stock adjustment, low-stock threshold, bulk operations

---

## Notification.UnitTests (4 files, ~15 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `Consumers/OrderStatusChangedConsumerTests.cs` | Consumer | OrderStatusChangedEvent consumer |
| `Consumers/OrderCompletedConsumerTests.cs` | Consumer | OrderCompletedEvent consumer |
| `Consumers/OrderCancelledConsumerTests.cs` | Consumer | OrderCancelledEvent consumer |
| `Consumers/UserIdProviderTests.cs` | Consumer | SignalR UserIdProvider |

**Gaps:** Email notification delivery, notification preferences

---

## Ordering.UnitTests (6 files, ~35 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `Domain/OrderTests.cs` | Domain | Order entity behavior |
| `Domain/OrderItemTests.cs` | Domain | OrderItem entity behavior |
| `Domain/AddressTests.cs` | Domain | Address value object |
| `Application/CreateOrderHandlerTests.cs` | Application | CreateOrderCommand handler |
| `Application/GetOrderByIdHandlerTests.cs` | Application | GetOrderByIdQuery handler |
| `Application/CancelOrderHandlerTests.cs` | Application | CancelOrderCommand handler |
| `Application/UpdateOrderStatusHandlerTests.cs` | Application | UpdateOrderStatusCommand handler |

**Gaps:** Saga orchestration, order items validation

---

## Payment.UnitTests (5 files, ~25 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `Domain/PaymentTransactionTests.cs` | Domain | PaymentTransaction entity, status transitions |
| `Domain/RefundTests.cs` | Domain | Refund entity behavior |
| `Application/ProcessPaymentHandlerTests.cs` | Application | ProcessPaymentCommand handler |
| `Application/RefundPaymentHandlerTests.cs` | Application | RefundPaymentCommand handler |
| `Infrastructure/MockPaymentGatewayTests.cs` | Infrastructure | MockPaymentGateway success/failure |

**Gaps:** Idempotency, amount validation (negative/zero)

---

## Search.UnitTests (3 files, ~12 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `Consumers/ProductCreatedConsumerTests.cs` | Consumer | ProductCreatedEvent consumer |
| `Consumers/ProductUpdatedConsumerTests.cs` | Consumer | ProductUpdatedEvent consumer |
| `Consumers/ProductDeletedConsumerTests.cs` | Consumer | ProductDeletedEvent consumer |

**Gaps:** Search query logic, facet aggregation, indexing error handling

---

## StoreManagement.UnitTests (6 files, ~25 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `StoreTests.cs` | Domain | Store entity behavior |
| `CreateStoreHandlerTests.cs` | Application | CreateStoreCommand handler |
| `UpdateStoreHandlerTests.cs` | Application | UpdateStoreCommand handler |
| `GetStoreByIdHandlerTests.cs` | Application | GetStoreByIdQuery handler |
| `ListStoresHandlerTests.cs` | Application | ListStoresQuery handler |
| `VerifySellerHandlerTests.cs` | Application | VerifySellerCommand handler |

**Gaps:** Store deactivation, seller store ownership validation

---

## ApiGateway.UnitTests (3 files, ~3 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `Middleware/CsrfValidationMiddlewareTests.cs` | Middleware | CSRF token validation |
| `Middleware/CookieToBearerMiddlewareTests.cs` | Middleware | Cookie-to-Bearer token conversion |
| `Helpers/TestAuthenticationService.cs` | — | Test helper for auth service |

---

## BuildingBlocks (3 files, ~5 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `Models/ResultTests.cs` | Models | Result<T> pattern |
| `Models/PagedResultTests.cs` | Models | PagedResult<T> |
| `AggregateRootTests.cs` | Domain | AggregateRoot base class |

---

## How to Run

```bash
# All backend unit tests
dotnet test tests/UnitTests/ --verbosity normal

# Single service
dotnet test tests/UnitTests/Cart.UnitTests/ --verbosity normal

# With coverage
dotnet test tests/UnitTests/ --collect:"XPlat Code Coverage"
```

---

*Generated from test source files in `tests/UnitTests/`.*
