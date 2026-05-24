# Plan 07: Search & Discovery Enhancements

## Goal
Enhance search with powerful faceted filtering, dynamic breadcrumbs, autocomplete, and saved searches with price alerts.

## Context
- **Current state:** Search.API has GET /api/search/products with q, categoryId, priceMin, priceMax, tags, page, pageSize. Frontend has SearchFacetsComponent with price range and category facets.
- **Target state:** Deep faceted filtering with brand/attribute checkboxes, dynamic breadcrumbs, autocomplete in search bar, saved searches with notifications.
- **Design ref:** `plans/future_design/search_and_discovery.md`

## Prerequisites
- Search.API uses Elasticsearch — exists
- Catalog.API has categories and products — exists
- Frontend has CatalogStore with searchProducts — exists

## Backend Changes

### 1. Enhance Elasticsearch Index with More Facets
**File:** `src/Microservices/Search/Search.API/Models/ProductSearchDocument.cs`

Add fields for richer faceting:
```csharp
public class ProductSearchDocument
{
    // Existing: Id, Name, Description, Price, CategoryId, CategoryName, Tags, Sku, ImageUrl
    public string? Brand { get; set; }        // NEW
    public Dictionary<string, string> Attributes { get; set; } = []; // NEW (e.g., {"Color": "Red", "Size": "XL"})
    public double? Rating { get; set; }       // NEW (average rating)
    public int ReviewCount { get; set; }      // NEW
    public bool InStock { get; set; }         // NEW
}
```

### 2. Update Indexing Consumers
**File:** `src/Microservices/Search/Search.API/Services/ProductIndexingService.cs`

Update ProductCreatedConsumer and ProductUpdatedConsumer to include new fields.

### 3. Add Aggregations to Search Query
**File:** `src/Microservices/Search/Search.API/Services/ElasticsearchSearchService.cs`

Return facets alongside results:
```csharp
public async Task<SearchResult<ProductSearchDocument>> SearchAsync(...)
{
    var response = await _client.SearchAsync<ProductSearchDocument>(s => s
        .Query(q => /* existing query */)
        .Aggregations(a => a
            .Terms("categories", t => t.Field(f => f.CategoryId))
            .Terms("brands", t => t.Field(f => f.Brand))
            .Range("price_ranges", r => r.Field(f => f.Price)
                .Ranges(
                    rr => rr.To(25),
                    rr => rr.From(25).To(50),
                    rr => rr.From(50).To(100),
                    rr => rr.From(100).To(250),
                    rr => rr.From(250)
                ))
            .Average("avg_rating", avg => avg.Field(f => f.Rating))
        )
    );
    
    return new SearchResult<ProductSearchDocument>
    {
        Items = response.Documents,
        Total = response.Total,
        Facets = ParseAggregations(response.Aggregations) // NEW
    };
}
```

### 4. Update Search Response Model
**File:** `src/Microservices/Search/Search.API/Models/SearchResult.cs`

Add facets:
```csharp
public class SearchResult<T>
{
    public IReadOnlyList<T> Items { get; set; }
    public long Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public SearchFacets Facets { get; set; } // NEW
}

public class SearchFacets
{
    public List<FacetValue> Categories { get; set; } = [];
    public List<FacetValue> Brands { get; set; } = [];
    public List<FacetValue> PriceRanges { get; set; } = [];
    public double? AverageRating { get; set; }
}

public class FacetValue
{
    public string Key { get; set; }
    public long Count { get; set; }
}
```

### 5. Update Search Endpoint
**File:** `src/Microservices/Search/Search.API/Endpoints/SearchEndpoints.cs`

Add new query parameters:
```csharp
group.MapGet("/products", async (
    string? q,
    Guid? categoryId,
    decimal? priceMin,
    decimal? priceMax,
    string? tags,
    string? brand,        // NEW
    double? minRating,    // NEW
    bool? inStock,        // NEW
    int page,
    int pageSize,
    ISearchService searchService,
    CancellationToken ct) => { ... });
```

### 6. Add Saved Search Feature
**New files:**
- `Identity.Domain/Aggregates/SavedSearch.cs` (UserId, Query, Filters JSON, CreatedAt)
- `Identity.Application/Commands/SaveSearch/SaveSearchCommand.cs` + Handler
- `Identity.Application/Commands/DeleteSavedSearch/DeleteSavedSearchCommand.cs` + Handler
- `Identity.Application/Queries/ListSavedSearches/ListSavedSearchesQuery.cs` + Handler
- `Identity.API/Endpoints/SavedSearchEndpoints.cs`

```csharp
// Identity.API endpoints
group.MapPost("/saved-searches", async (SaveSearchCommand cmd, ...) => { ... });
group.MapDelete("/saved-searches/{id:guid}", async (Guid id, ...) => { ... });
group.MapGet("/saved-searches", async (...) => { ... });
```

### 7. Add Price Alert Notification
**New file:** `Notification.Worker/Consumers/PriceAlertConsumer.cs`

Periodically check saved searches against new/updated products. When a match is found at a lower price, send notification via SignalR.

## Frontend Changes

### 8. Enhance SearchFacetsComponent
**File:** `src/web/src/app/features/catalog/components/search-facets/search-facets.ts`

Add:
- Brand checkboxes with item counts (from facets)
- Rating filter (clickable stars: 4+, 3+, 2+, 1+)
- "In Stock Only" toggle
- Category breadcrumb facets
- "Clear all filters" button
- Show active filters as removable chips

### 9. Create Dynamic Breadcrumbs Component
**New file:** `src/web/src/app/shared/components/breadcrumbs/breadcrumbs.ts`

- Home > Electronics > Laptops > Gaming Laptops
- Each node clickable (navigates to that category)
- Hovering a node shows dropdown of sibling categories
- Updates as user filters/navigates

### 10. Add Autocomplete to Search Bar
**File:** `src/web/src/app/shared/components/header/header.ts` (or new search component)

- Debounced (300ms) suggestions as user types
- Shows product names, category names, recent searches
- Click suggestion → navigate to product or search
- "See all results for 'query'" link at bottom

### 11. Create Search Bar Component
**New file:** `src/web/src/app/shared/components/search-bar/search-bar.ts`

Extract search bar into reusable component:
- Input with autocomplete dropdown
- Search button
- Camera icon (for future visual search)
- Recent searches (localStorage)
- Suggestions from Search.API

### 12. Update CatalogStore for Faceted Search
**File:** `src/web/src/app/features/catalog/catalog.store.ts`

Add facet state:
```typescript
interface CatalogState {
  // ... existing
  facets: SearchFacets | null;
  activeFilters: {
    categoryId?: string;
    brand?: string[];
    minRating?: number;
    inStock?: boolean;
    priceMin?: number;
    priceMax?: number;
  };
}
```

### 13. Create Saved Searches Component
**New file:** `src/web/src/app/features/auth/profile/components/saved-searches/saved-searches.ts`

- List of saved search queries
- Each shows the query + filters
- "Set price alert" toggle
- "Delete" button
- "Run search" link

## Files to Modify/Create

| Action | File |
|--------|------|
| MODIFY | `Search.API/Models/ProductSearchDocument.cs` |
| MODIFY | `Search.API/Models/SearchResult.cs` |
| MODIFY | `Search.API/Services/ElasticsearchSearchService.cs` |
| MODIFY | `Search.API/Services/ProductIndexingService.cs` |
| MODIFY | `Search.API/Endpoints/SearchEndpoints.cs` |
| CREATE | `Identity.Domain/Aggregates/SavedSearch.cs` |
| CREATE | `Identity.Application/Commands/SaveSearch/` |
| CREATE | `Identity.Application/Queries/ListSavedSearches/` |
| CREATE | `Identity.API/Endpoints/SavedSearchEndpoints.cs` |
| MODIFY | `src/web/src/app/features/catalog/components/search-facets/search-facets.ts` |
| MODIFY | `src/web/src/app/features/catalog/catalog.store.ts` |
| MODIFY | `src/web/src/app/features/catalog/catalog.models.ts` |
| CREATE | `src/web/src/app/shared/components/breadcrumbs/breadcrumbs.ts` |
| CREATE | `src/web/src/app/shared/components/search-bar/search-bar.ts` |
| MODIFY | `src/web/src/app/shared/components/header/header.ts` |
| CREATE | `src/web/src/app/features/auth/profile/components/saved-searches/saved-searches.ts` |

## Verification
1. `dotnet build Marketplace.slnx` — no errors
2. `ng build` — no errors
3. Manual: Search → autocomplete suggestions appear
4. Manual: Search results → facets show brands, ratings, price ranges
5. Manual: Click facet → results update instantly
6. Manual: Breadcrumbs show category path
7. Manual: Save search → appears in profile
8. Manual: Mobile responsive facets (collapsible)
