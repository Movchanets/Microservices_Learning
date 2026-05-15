# Task: Implement Catalog Service Unit Tests

**Source Plan**: `implementation_plan/tests/catalog.unit_tests.md`

Goal: Implement unit tests for the Catalog domain and application layers using xUnit, Moq, and FluentAssertions.

Context: 
- Location: src/Microservices/Catalog/
- Target Project: tests/UnitTests/Catalog.UnitTests
- References: Catalog.Domain, Catalog.Application

Action Items:
1. Project Setup:
   - Verify/Create tests/UnitTests/Catalog.UnitTests.
   - Install NuGet packages: xunit, Moq, FluentAssertions.
2. Domain Layer Tests (Product & Category):
   - Test: Product.Create generates ProductCreatedDomainEvent.
   - Test: Product.UpdatePrice throws exception for negative prices.
   - Test: Product.UpdatePrice generates ProductPriceChangedDomainEvent.
   - Test: Category.Create validates inputs correctly.
3. Application Layer (Commands/Queries):
   - Test: CreateProductCommandHandler persists product and returns Id.
   - Test: UpdateProductPriceCommandHandler retrieves and updates price.
   - Test: DeleteProductCommandHandler marks product as deleted/removes it.
   - Test: GetProductsQueryHandler correctly filters products using mock repository.
   - Test: GetCategoriesQueryHandler returns active categories.

Validation:
- Run: dotnet test tests/UnitTests/Catalog.UnitTests/Catalog.UnitTests.csproj
