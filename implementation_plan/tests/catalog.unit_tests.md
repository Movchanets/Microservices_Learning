# Catalog Service — Unit Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Catalog/`

## Goal
Implement unit tests for the Catalog domain and application layers using xUnit, Moq, and FluentAssertions.

## Scope
- **In**: `Product` and `Category` aggregates, `Money` value objects, MediatR handlers.
- **Out**: Elasticsearch indexing, Postgres querying.

## Action Items

[ ] **Step 1: Set up Project**
  - Verify/Create `tests/UnitTests/Catalog.UnitTests` referencing `Catalog.Domain` and `Catalog.Application`.
  - Ensure packages are installed: `xunit`, `Moq`, `FluentAssertions`.

[ ] **Step 2: Domain Layer Tests (`Product` & `Category`)**
  - Test: `Product.Create` generates `ProductCreatedDomainEvent`.
  - Test: `Product.UpdatePrice` throws exception for negative prices.
  - Test: `Product.UpdatePrice` generates `ProductPriceChangedDomainEvent`.
  - Test: `Category.Create` validates inputs correctly.

[ ] **Step 3: Application Layer Tests (Commands)**
  - Test: `CreateProductCommandHandler` persists product and returns Id.
  - Test: `UpdateProductPriceCommandHandler` retrieves product and updates price.
  - Test: `DeleteProductCommandHandler` marks product as deleted/removes it.

[ ] **Step 4: Application Layer Tests (Queries)**
  - Test: `GetProductsQueryHandler` correctly filters products using mock repository.
  - Test: `GetCategoriesQueryHandler` returns active categories.

[ ] **Step 5: Validation**
  - Run `dotnet test tests/UnitTests/Catalog.UnitTests/Catalog.UnitTests.csproj`.