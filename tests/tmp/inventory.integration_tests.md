# Task: Implement Inventory Service Integration Tests

**Source Plan**: `implementation_plan/tests/inventory.integration_tests.md`

Goal: Implement integration tests for the Inventory service testing PostgreSQL repository methods, optimistic concurrency, and MassTransit consumers using Testcontainers.

Context: 
- Location: src/Microservices/Inventory/
- Target Project: tests/IntegrationTests/Inventory.IntegrationTests
- References: Inventory.Infrastructure

Action Items:
1. Project Setup:
   - Verify/Create tests/IntegrationTests/Inventory.IntegrationTests.
   - Install NuGet packages: xunit, FluentAssertions, Testcontainers.PostgreSql, MassTransit.TestFramework.
2. Test Fixture Setup:
   - Create InventoryDatabaseFixture utilizing PostgreSqlContainer and apply migrations.
3. Integration Tests:
   - Test: Save and retrieve an InventoryItem.
   - Test: Concurrent modification (RowVersion). Verify DbUpdateConcurrencyException when two contexts modify the same item.
4. Consumer Integration:
   - Test: ProductCreatedConsumer processes event and writes a new InventoryItem (0 stock).
   - Test: ReserveInventoryConsumer reserves stock and publishes InventoryReservedEvent.

Validation:
- Run: dotnet test tests/IntegrationTests/Inventory.IntegrationTests/Inventory.IntegrationTests.csproj
