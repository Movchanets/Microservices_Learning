# Task: Implement Cart Service Unit Tests

**Source Plan**: `implementation_plan/tests/cart.unit_tests.md`

Goal: Implement unit tests for the Cart domain and application layers using xUnit, Moq, and FluentAssertions.

Context: 
- Location: src/Microservices/Cart/
- Target Project: tests/UnitTests/Cart.UnitTests
- References: Cart.Domain, Cart.Application

Action Items:
1. Project Setup:
   - Verify/Create tests/UnitTests/Cart.UnitTests.
   - Install NuGet packages: xunit, Moq, FluentAssertions.
2. Domain Layer Tests (ShoppingCart Aggregate):
   - Test: AddItem adds new item vs. increments quantity.
   - Test: UpdateQuantity modifies quantity or removes item if <= 0.
   - Test: Clear removes all items.
3. Application Layer (Commands/Queries):
   - Test: GetCartQueryHandler returns cart from mock repository.
   - Test: UpdateCartCommandHandler updates items and saves.
   - Test: CheckoutCartCommandHandler fails if cart is empty.
   - Test: CheckoutCartCommandHandler succeeds, publishes OrderSubmittedEvent, and deletes cart.

Validation:
- Run: dotnet test tests/UnitTests/Cart.UnitTests/Cart.UnitTests.csproj
