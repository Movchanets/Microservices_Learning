# Catalog Service — Integration Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Catalog/`

## Goal
Implement integration tests for the Catalog service testing PostgreSQL repository methods and MassTransit Outbox functionality using Testcontainers.

## Scope
- **In**: `ProductRepository`, `CategoryRepository`, Outbox message persistence.
- **Out**: UI testing, Search service.

## Action Items

[ ] **Step 1: Set up Project**
  - Verify/Create `tests/IntegrationTests/Catalog.IntegrationTests` referencing `Catalog.Infrastructure`.
  - Install packages: `xunit`, `FluentAssertions`, `Testcontainers.PostgreSql`.

[ ] **Step 2: Test Fixture Setup**
  - Create `CatalogDatabaseFixture` utilizing `PostgreSqlContainer`.
  - Apply `CatalogDbContext` migrations on startup.

[ ] **Step 3: Repository Integration Tests**
  - Test: Create a Product and retrieve it verifying all fields (including Price/Currency).
  - Test: Pagination and filtering queries directly against the database context.
  - Test: Category hierarchy (Parent/Child relationships) persistence.

[ ] **Step 4: Outbox Integration Tests**
  - Test: Verify that calling `uow.SaveChangesAsync()` after domain event generation properly writes to MassTransit Outbox tables inside PostgreSQL.

[ ] **Step 5: Validation**
  - Run `dotnet test tests/IntegrationTests/Catalog.IntegrationTests/Catalog.IntegrationTests.csproj`.