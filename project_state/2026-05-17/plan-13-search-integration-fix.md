# Plan 13: Search Integration Test Fix

## Goal
Fix all 6 failing `Search.IntegrationTests` by adding Elasticsearch via Testcontainers, matching the existing pattern for PostgreSQL, RabbitMQ, and Redis.

## Context
- **Current state:** 6 Search.IntegrationTests all fail with "Sequence contains no matching element" because Elasticsearch is not running in the test environment. Tests try to index and query documents but get empty results.
- **Target state:** All 6 Search.IntegrationTests pass using an ephemeral Elasticsearch container spun up by Testcontainers.
- **Root cause:** `SearchFixture` doesn't provision Elasticsearch. Tests assume ES is running locally.

## Prerequisites
- Testcontainers 4.11.0 already in use (PostgreSQL, RabbitMQ, Redis) — `tests/IntegrationTests/Shared/`
- `IntegrationTests.Shared` has base fixture patterns — exists
- Search.API uses `Elasticsearch.Net` / `NEST` client — exists

## Backend Changes

### 1. Add Elasticsearch Testcontainer Package
**File:** `tests/IntegrationTests/Search.IntegrationTests/Search.IntegrationTests.csproj`

```xml
<PackageReference Include="Testcontainers.Elasticsearch" Version="4.11.0" />
```

### 2. Create SearchFixture
**File:** `tests/IntegrationTests/Search.IntegrationTests/Fixtures/SearchFixture.cs`

```csharp
public sealed class SearchFixture : IAsyncLifetime
{
    private readonly ElasticsearchContainer _elasticsearchContainer = new ElasticsearchBuilder()
        .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.12.0")
        .WithEnvironment("discovery.type", "single-node")
        .WithEnvironment("xpack.security.enabled", "false")
        .WithPortBinding(9200, true)
        .Build();

    public string ElasticsearchUrl => _elasticsearchContainer.GetConnectionString();

    public IElasticClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _elasticsearchContainer.StartAsync();

        var settings = new ConnectionSettings(new Uri(ElasticsearchUrl))
            .DefaultIndex("products")
            .DefaultMappingFor<ProductDocument>(m => m.IdProperty(p => p.Id));

        Client = new ElasticClient(settings);

        // Create index with mapping
        await Client.Indices.CreateAsync("products", c => c
            .Map<ProductDocument>(m => m
                .AutoMap()
                .Properties(ps => ps
                    .Text(t => t.Name(n => n.Name).Analyzer("standard"))
                    .Text(t => t.Name(n => n.Description).Analyzer("standard"))
                    .Keyword(k => k.Name(n => n.CategoryId))
                    .Keyword(k => k.Name(n => n.Sku))
                    .Number(n => n.Name(n => n.Price).Type(NumberType.Double))
                    .Boolean(b => b.Name(n => n.IsActive))
                )
            )
        );
    }

    public async Task DisposeAsync()
    {
        await _elasticsearchContainer.StopAsync();
    }
}
```

### 3. Update Existing Test Classes to Use Fixture
**File:** `tests/IntegrationTests/Search.IntegrationTests/IndexingTests.cs`

Update constructor to accept `SearchFixture` and use `SearchFixture.Client` instead of assuming a running ES instance.

**File:** `tests/IntegrationTests/Search.IntegrationTests/SearchQueryTests.cs`

Same — use `SearchFixture.Client` for indexing test data and querying.

### 4. Add Collection Fixture
**File:** `tests/IntegrationTests/Search.IntegrationTests/CollectionDefinition.cs`

```csharp
[CollectionDefinition("SearchCollection")]
public class SearchCollectionDefinition : ICollectionFixture<SearchFixture> { }
```

Decorate test classes with `[Collection("SearchCollection")]`.

### 5. Add Refresh After Indexing
The tests likely fail because ES doesn't refresh indices immediately after indexing. Add `Refresh(Indices.All)` after bulk/index operations in test setup.

```csharp
await client.IndexAsync(document, idx => idx.Index("products"));
await client.Indices.RefreshAsync("products");  // Critical for test reliability
```

## E2E Verification

### Spec File: `tests/E2ETests/tests/search-product-lifecycle.spec.ts`

**Scenario:** Seller creates product. Buyer searches for it. Product appears in results.

```
TEST: search-product-lifecycle.spec.ts

Setup:
  1. Register seller via API, create store, verify
  2. Register buyer via API

Test: "product appears in search after creation"
  3. Login as seller in browser
  4. Navigate to /seller → Products tab
  5. Click "Add Product"
  6. Fill in: name="E2E Search Test Widget", description="A searchable widget", price=42.99, sku
  7. Submit → wait for success
  8. Logout seller

  9. Login as buyer in browser
  10. Navigate to /catalog
  11. Type "Search Test Widget" in search bar
  12. Wait for results (debounce 350ms + network)
  13. Verify product "E2E Search Test Widget" appears in results
  14. Click product → verify detail page shows correct name, price, description

Test: "search returns empty for non-existent product"
  15. Search for "xyznonexistent123"
  16. Verify empty state or "No products found" message
```

### New Page Objects
- None — uses existing `CatalogPage` and `ProductDetailPage`

### Files to Create/Modify
```
tests/E2ETests/tests/search-product-lifecycle.spec.ts     # NEW
```

## Acceptance Criteria
- [ ] `Testcontainers.Elasticsearch` 4.11.0 added to Search.IntegrationTests.csproj
- [ ] `SearchFixture` provisions ephemeral Elasticsearch container
- [ ] `IndexingTests.IndexProduct_CanBeRetrievedById` passes
- [ ] `IndexingTests.UpdateProduct_VerifyNewFields` passes
- [ ] `SearchQueryTests.FullTextSearch_ReturnsMatchingProducts` passes
- [ ] `SearchQueryTests.FilterByCategory_ReturnsOnlyMatchingProducts` passes
- [ ] `SearchQueryTests.Pagination_ReturnsCorrectPage` passes
- [ ] `SearchQueryTests.PriceRangeFilter_ReturnsProductsInRange` passes
- [ ] All 6/6 Search.IntegrationTests pass
- [ ] E2E test passes: product created → searchable → clickable
- [ ] All other integration tests still pass

## Verification Commands
```bash
dotnet build Marketplace.slnx
dotnet test tests/IntegrationTests/Search.IntegrationTests/ --verbosity normal
npx playwright test tests/E2ETests/tests/search-product-lifecycle.spec.ts
```
