# Task: Implement Inventory Service Unit Tests

**Source Plan**: `implementation_plan/tests/inventory.unit_tests.md`

Goal: Implement unit tests for the Inventory domain and application layers using xUnit, Moq, and FluentAssertions.

Context: 
- Location: src/Microservices/Inventory/
- Target Project: tests/UnitTests/Inventory.UnitTests
- References: Inventory.Domain, Inventory.Application

Action Items:
1. Project Setup:
   - Verify/Create tests/UnitTests/Inventory.UnitTests.
   - Install NuGet packages: xunit, Moq, FluentAssertions.
2. Domain Layer Tests (InventoryItem Aggregate):
   - Test: InventoryItem.Create initializes with correct SKU and quantity.
   - Test: Reserve deducts quantity and generates StockReservedDomainEvent.
   - Test: Reserve throws OutOfStockException when quantity exceeds available stock.
   - Test: Release adds quantity back and generates StockReleasedDomainEvent.
3. Application Layer (Commands):
   - Test: ReserveStockCommandHandler successfully reserves items and commits when stock is available.
   - Test: ReserveStockCommandHandler returns a failed Result when any item is out of stock.
   - Test: ReleaseStockCommandHandler successfully calls Release on existing items.

Validation:
- Run: dotnet test tests/UnitTests/Inventory.UnitTests/Inventory.UnitTests.csproj
