# Cart Service — Unit Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Cart/`

## Goal
Implement unit tests for the Cart domain and application layers using xUnit, Moq, and FluentAssertions.

## Scope
- **In**: `ShoppingCart` aggregate, MediatR handlers.
- **Out**: Redis caching, PostgreSQL querying.

## Action Items

[ ] **Step 1: Set up Project**
  - Verify/Create `tests/UnitTests/Cart.UnitTests` referencing `Cart.Domain` and `Cart.Application`.
  - Ensure packages are installed: `xunit`, `Moq`, `FluentAssertions`.

[ ] **Step 2: Domain Layer Tests (`ShoppingCart` Aggregate)**
  - Test: `AddItem` adds a new item when SKU does not exist.
  - Test: `AddItem` increments quantity when SKU already exists.
  - Test: `UpdateQuantity` modifies quantity or removes item if quantity <= 0.
  - Test: `Clear` removes all items from the cart.

[ ] **Step 3: Application Layer Tests (Commands/Queries)**
  - Test: `GetCartQueryHandler` returns cart from mock repository.
  - Test: `UpdateCartCommandHandler` updates items and saves to repository.
  - Test: `CheckoutCartCommandHandler` fails if cart is empty.
  - Test: `CheckoutCartCommandHandler` succeeds, publishes `OrderSubmittedEvent`, and deletes cart.

[ ] **Step 4: Validation**
  - Run `dotnet test tests/UnitTests/Cart.UnitTests/Cart.UnitTests.csproj`.