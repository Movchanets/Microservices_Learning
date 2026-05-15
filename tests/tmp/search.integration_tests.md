# Task: Implement Search Service Integration Tests

**Source Plan**: `implementation_plan/tests/search.integration_tests.md`

Goal: Implement integration tests for the Search service testing actual Elasticsearch indexing and searching via Testcontainers.

Context: 
- Location: src/Microservices/Search/
- Target Project: tests/IntegrationTests/Search.IntegrationTests
- References: Search.API

Action Items:
1. Project Setup:
   - Verify/Create tests/IntegrationTests/Search.IntegrationTests.
   - Install NuGet packages: xunit, FluentAssertions, Testcontainers.Elasticsearch.
2. Test Fixture Setup:
   - Create SearchDatabaseFixture utilizing ElasticsearchBuilder.
   - Ensure the Elasticsearch index (products) is created with correct mappings/analyzers on startup.
3. Integration Tests:
   - Test: Index a new product and verify retrieval.
   - Test: Update document and verify fields.
   - Test: Full-text search on names/descriptions.
   - Test: Filtering by CategoryId.
   - Test: Pagination (skip/take) over 10+ seeded documents.

Validation:
- Run: dotnet test tests/IntegrationTests/Search.IntegrationTests/Search.IntegrationTests.csproj
