# Catalog Feature

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/features/catalog/` |
| **Store Scope** | `CatalogStore` — feature-scoped (NOT `providedIn: 'root'`) |
| **Additional Stores** | `ProductDetailStore` (feature-scoped), `ReviewStore` (feature-scoped) |
| **Route Prefix** | `/catalog` |
| **Render Mode** | `RenderMode.Server` (SSR) |

## Component Structure

```
catalog/
├── catalog.store.ts              # CatalogStore (feature-scoped)
├── catalog.service.ts            # HTTP service → BFF gateway
├── catalog.models.ts             # ProductListItem, Category, FacetValue, PagedResult, SearchResult, Sku
├── catalog.routes.ts             # Named export: CATALOG_ROUTES
├── review.service.ts             # ReviewService → Review API
├── review.store.ts               # ReviewStore (feature-scoped)
├── product-list/
│   └── product-list.ts           # ProductListComponent — grid/list of products
├── product-detail/
│   ├── product-detail.ts         # ProductDetailComponent — PDP
│   ├── product-detail.html       # External template
│   ├── product-detail.store.ts   # ProductDetailStore (feature-scoped)
│   └── product-detail.css
└── components/
    ├── product-card/
    │   └── product-card.ts       # ProductCardComponent — card in grid
    ├── buy-box/
    │   ├── buy-box.ts            # BuyBoxComponent — price + add-to-cart
    │   └── buy-box.spec.ts       # ✅ Tests
    ├── frequently-bought-together/
    │   ├── frequently-bought-together.ts
    │   └── frequently-bought-together.spec.ts  # ✅ Tests
    ├── category-sidebar/
    │   └── category-sidebar.ts   # CategorySidebarComponent — category tree
    ├── search-facets/
    │   └── search-facets.ts      # SearchFacetsComponent — brand, price, rating filters
    ├── pagination/
    │   └── pagination.ts         # PaginationComponent
    ├── write-review/
    │   └── write-review.ts       # WriteReviewComponent — form for new review
    ├── review-list/
    │   └── review-list.ts        # ReviewListComponent — paginated reviews
    └── review-summary/
        └── review-summary.ts     # ReviewSummaryComponent — rating distribution
```

## SignalStore State Management

### CatalogStore (feature-scoped)

| State Property | Type | Description |
|:---|:---|:---|
| `products` | `ProductListItem[]` | Current page of products |
| `categories` | `Category[]` | Category tree for sidebar |
| `facets` | `Record<string, FacetValue[]>` | Search facets from Search.API |
| `page` / `pageSize` / `totalCount` | `number` | Pagination state |
| `searchQuery` | `string` | Free-text search |
| `selectedCategoryId` | `string \| null` | Active category filter |
| `priceMin` / `priceMax` | `number \| null` | Price range filter |
| `selectedBrands` | `string[]` | Brand facet filter |
| `minRating` | `number \| null` | Minimum rating filter |
| `inStockOnly` | `boolean` | In-stock filter |
| `loading` | `boolean` | Loading state |
| `error` | `string \| null` | Error message |

**Computed signals:** `totalPages`, `hasPrevious`, `hasNext`, `isSearchMode`, `activeCategory`

**Key methods:** `loadProducts()`, `searchProducts()`, `loadCategories()`, `refresh()`, `updateSearchQuery()`, `selectCategory()`, `setPriceRange()`, `toggleBrand()`, `setMinRating()`, `setInStockOnly()`, `clearFilters()`, `goToPage()`

### ProductDetailStore (feature-scoped)

| State Property | Type | Description |
|:---|:---|:---|
| `product` | `Product \| null` | Full product with SKUs |
| `storeInfo` | `StoreSettings \| null` | Seller store info |
| `stockQuantity` | `number \| null` | First SKU stock level |
| `recommendations` | `ProductListItem[]` | Related products |

**Key methods:** `loadProduct(id)`, `loadStock(sku)`, `loadRecommendations(productId)`, `loadStoreInfo(storeId)`

### ReviewStore (feature-scoped)

| State Property | Type | Description |
|:---|:---|:---|
| `reviews` | `Review[]` | Paginated reviews |
| `summary` | `ReviewSummary \| null` | Rating distribution |
| `sort` | `string` | Sort order (default: `helpful`) |
| `ratingFilter` | `number \| null` | Filter by star rating |
| `submitting` | `boolean` | Review submission state |

**Key methods:** `loadReviews(productId)`, `loadSummary(productId)`, `createReview(productId, data)`, `voteReview(productId, reviewId, isHelpful)`, `setSort()`, `setRatingFilter()`, `goToPage()`

## Key Routes

| Path | Component | Guard |
|:---|:---|:---|
| `/catalog` | `ProductListComponent` | None (public) |
| `/catalog/:id` | `ProductDetailComponent` | None (public) |

## Test Coverage Status

| Spec File | Tests | Status |
|:---|:---|:---|
| `components/buy-box/buy-box.spec.ts` | ✅ | Passing |
| `components/frequently-bought-together/frequently-bought-together.spec.ts` | ✅ | Passing |
| `catalog.store.ts` | ❌ | **No tests** |
| `catalog.service.ts` | ❌ | **No tests** |
| `review.store.ts` | ❌ | **No tests** |
| `review.service.ts` | ❌ | **No tests** |
| `product-detail.store.ts` | ❌ | **No tests** |

**E2E Coverage:** Partially covered — `browse-products.spec.ts` (~4 tests), `catalog-filter-sort.spec.ts` (~6 tests). Missing: product detail, write review, search facets, API failure states.

## Known Gaps / Issues

- **Store/service tests missing:** `CatalogStore`, `CatalogService`, `ReviewStore`, `ReviewService`, `ProductDetailStore` all have 0 unit tests.
- **No sort option in CatalogStore:** Sort is only available in `ReviewStore`. Product list sorting relies entirely on API defaults.
- **`isSearchMode` computation:** Dual-API routing (Catalog.API vs Search.API) is implicit — `refresh()` checks `searchQuery` length, but other filter changes don't always trigger the right API.
- **Category sidebar tree:** Uses `CategoryTreeService` from core — no deep-linking or URL-based category selection.
- **Review voting:** No optimistic update — UI waits for server response before reflecting vote change.
