# Search Service — Unit Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Search/`

## Goal
Implement unit tests for the Search service application logic and mapping.

## Scope
- **In**: MassTransit Consumers, query mapping logic.
- **Out**: Actual Elasticsearch nodes.

## Action Items

[ ] **Step 1: Set up Project**
  - Verify/Create `tests/UnitTests/Search.UnitTests` referencing `Search.API` (or extracted Application layer if it exists).
  - Install packages: `xunit`, `Moq`, `FluentAssertions`, `MassTransit.TestFramework`.

[ ] **Step 2: Consumer Tests**
  - Test: `ProductCreatedConsumer` maps the event correctly and calls the Elasticsearch client mock.
  - Test: `ProductUpdatedConsumer` updates the correct document ID.
  - Test: `ProductDeletedConsumer` issues a delete command to the index.

[ ] **Step 3: Query Building Tests**
  - Test: Ensure search endpoint parameters (query, filters, pagination) map to the expected Elasticsearch query descriptor structure (if isolated).

[ ] **Step 4: Validation**
  - Run `dotnet test tests/UnitTests/Search.UnitTests/Search.UnitTests.csproj`.