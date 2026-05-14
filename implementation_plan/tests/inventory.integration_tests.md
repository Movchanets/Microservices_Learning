# Inventory Service — Integration Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Inventory/`

## Goal
Implement integration tests for the Inventory service testing PostgreSQL repository methods, optimistic concurrency, and MassTransit consumers using Testcontainers.

## Scope
- **In**: `InventoryItemRepository`, EF Core optimistic concurrency (RowVersion), MassTransit Consumers.
- **Out**: UI testing, external microservices.

## Action Items

[ ] **Step 1: Set up Project**
  - Verify/Create `tests/IntegrationTests/Inventory.IntegrationTests` referencing `Inventory.Infrastructure`.
  - Install packages: `xunit`, `FluentAssertions`, `Testcontainers.PostgreSql`, `MassTransit.TestFramework`.

[ ] **Step 2: Test Fixture Setup**
  - Create `InventoryDatabaseFixture` utilizing `PostgreSqlContainer`.
  - Apply `InventoryDbContext` migrations on startup.

[ ] **Step 3: Repository & Concurrency Tests**
  - Test: Save and retrieve an `InventoryItem`.
  - Test: Concurrent modification. Save an item, retrieve it twice in different contexts, modify one and save, then attempt to modify and save the second. It should throw `DbUpdateConcurrencyException`.

[ ] **Step 4: Consumer Integration Tests**
  - Test: `ProductCreatedConsumer` properly processes event and writes a new `InventoryItem` with 0 stock to the database.
  - Test: `ReserveInventoryConsumer` correctly reserves stock and publishes `InventoryReservedEvent` to the in-memory bus.

[ ] **Step 5: Validation**
  - Run `dotnet test tests/IntegrationTests/Inventory.IntegrationTests/Inventory.IntegrationTests.csproj`.