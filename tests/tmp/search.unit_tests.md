# Task: Implement Search Service Unit Tests

**Source Plan**: `implementation_plan/tests/search.unit_tests.md`

Goal: Implement unit tests for the Search service application logic and mapping.

Context: 
- Location: src/Microservices/Search/
- Target Project: tests/UnitTests/Search.UnitTests
- References: Search.API (or Application layer)

Action Items:
1. Project Setup:
   - Verify/Create tests/UnitTests/Search.UnitTests.
   - Install NuGet packages: xunit, Moq, FluentAssertions, MassTransit.TestFramework.
2. Consumer Tests:
   - Test: ProductCreatedConsumer maps event correctly and calls Elasticsearch client mock.
   - Test: ProductUpdatedConsumer updates the correct document ID.
   - Test: ProductDeletedConsumer issues a delete command to the index.
3. Query Building:
   - Test: Ensure search endpoint parameters map to the expected Elasticsearch query descriptor structure.

Validation:
- Run: dotnet test tests/UnitTests/Search.UnitTests/Search.UnitTests.csproj
