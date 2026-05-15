# Task: Implement Catalog Service Integration Tests

**Source Plan**: `implementation_plan/tests/catalog.integration_tests.md`

Goal: Implement integration tests for the Catalog service testing PostgreSQL repository methods and MassTransit Outbox functionality using Testcontainers.

Context: 
- Location: src/Microservices/Catalog/
- Target Project: tests/IntegrationTests/Catalog.IntegrationTests
- References: Catalog.Infrastructure

Action Items:
1. Project Setup:
   - Verify/Create tests/IntegrationTests/Catalog.IntegrationTests.
   - Install NuGet packages: xunit, FluentAssertions, Testcontainers.PostgreSql.
2. Test Fixture Setup:
   - Create CatalogDatabaseFixture utilizing PostgreSqlContainer.
   - Apply CatalogDbContext migrations on startup.
3. Integration Tests:
   - Test: Create a Product and retrieve it verifying all fields (including Price/Currency).
   - Test: Pagination and filtering queries directly against DB context.
   - Test: Category hierarchy (Parent/Child) persistence.
   - Test: Verify uow.SaveChangesAsync() writes to MassTransit Outbox tables after domain events.

Validation:
- Run: dotnet test tests/IntegrationTests/Catalog.IntegrationTests/Catalog.IntegrationTests.csproj
