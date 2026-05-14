# Search Service — Integration Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Search/`

## Goal
Implement integration tests for the Search service testing actual Elasticsearch indexing and searching capabilities via Testcontainers.

## Scope
- **In**: `Elasticsearch` nodes, document indexing, mapping, search relevancy.
- **Out**: UI testing, other microservices.

## Action Items

[ ] **Step 1: Set up Project**
  - Verify/Create `tests/IntegrationTests/Search.IntegrationTests` referencing `Search.API`.
  - Install packages: `xunit`, `FluentAssertions`, `Testcontainers.Elasticsearch`.

[ ] **Step 2: Test Fixture Setup**
  - Create `SearchDatabaseFixture` utilizing `ElasticsearchBuilder`.
  - On startup, ensure the Elasticsearch index (`products`) is created with correct mappings (analyzers for fields).

[ ] **Step 3: Indexing Tests**
  - Test: Push a new product document directly to the client and verify it can be retrieved.
  - Test: Update an existing document and verify the new fields.

[ ] **Step 4: Search & Query Tests**
  - Test: Full-text search on product names and descriptions returns correct documents.
  - Test: Filtering by `CategoryId` returns exactly the subset of matching documents.
  - Test: Pagination (skip/take) works correctly over a seeded set of 10+ documents.

[ ] **Step 5: Validation**
  - Run `dotnet test tests/IntegrationTests/Search.IntegrationTests/Search.IntegrationTests.csproj`.