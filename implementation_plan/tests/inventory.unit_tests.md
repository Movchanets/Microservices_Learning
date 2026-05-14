# Inventory Service — Unit Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Inventory/`

## Goal
Implement unit tests for the Inventory domain and application layers using xUnit, Moq, and FluentAssertions.

## Scope
- **In**: `InventoryItem` aggregate, optimistic concurrency rules, MediatR handlers.
- **Out**: Postgres querying, MassTransit message dispatch.

## Action Items

[ ] **Step 1: Set up Project**
  - Verify/Create `tests/UnitTests/Inventory.UnitTests` referencing `Inventory.Domain` and `Inventory.Application`.
  - Ensure packages are installed: `xunit`, `Moq`, `FluentAssertions`.

[ ] **Step 2: Domain Layer Tests (`InventoryItem` Aggregate)**
  - Test: `InventoryItem.Create` initializes with correct SKU and quantity.
  - Test: `Reserve` deducts quantity and generates `StockReservedDomainEvent`.
  - Test: `Reserve` throws `OutOfStockException` when requested quantity exceeds available stock.
  - Test: `Release` adds quantity back and generates `StockReleasedDomainEvent`.

[ ] **Step 3: Application Layer Tests (Commands)**
  - Test: `ReserveStockCommandHandler` successfully reserves items and commits when stock is available.
  - Test: `ReserveStockCommandHandler` returns a failed `Result` when any item is out of stock.
  - Test: `ReleaseStockCommandHandler` successfully calls `Release` on existing items.

[ ] **Step 4: Validation**
  - Run `dotnet test tests/UnitTests/Inventory.UnitTests/Inventory.UnitTests.csproj`.