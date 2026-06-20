# Catalog Feature

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/features/catalog/` |
| **Store Scope** | `CatalogStore` — feature-scoped (NOT `providedIn: 'root'`) |
| **Additional Stores** | `ProductDetailStore` (feature-scoped) |
| **Route Prefix** | `/catalog` |
| **Render Mode** | `RenderMode.Server` (SSR) |

## Component Structure

```
catalog/
├── catalog.store.ts              # CatalogStore (feature-scoped)
├── catalog.service.ts            # HTTP service → BFF gateway
├── catalog.models.ts             # Product, ProductListItem, Sku, Category, FacetValue, PagedResult, SearchResult, VariantMatrix, GalleryItem, etc.
├── catalog.routes.ts             # Named export: CATALOG_ROUTES
├── product-list/
│   └── product-list.ts           # ProductListComponent — grid/list of products
├── product-detail/
│   ├── product-detail.ts         # ProductDetailComponent — PDP
│   ├── product-detail.html       # External template
│   ├── product-detail.css
│   ├── product-detail.spec.ts    # ✅ Tests
│   ├── product-detail.store.ts   # ProductDetailStore (feature-scoped)
│   └── product-detail.store.spec.ts  # ✅ Tests
└── components/
    ├── product-card/
    │   ├── product-card.ts       # ProductCardComponent — card in grid
    │   └── product-card.spec.ts  # ✅ Tests
    ├── buy-box/
    │   ├── buy-box.ts            # BuyBoxComponent — price + add-to-cart
    │   └── buy-box.spec.ts       # ✅ Tests
    ├── variant-picker/
    │   └── variant-picker.ts     # VariantPickerComponent — color/storage axis grid
    ├── image-gallery/
    │   └── image-gallery.ts      # ImageGalleryComponent — product/SKU image carousel
    ├── category-sidebar/
    │   └── category-sidebar.ts   # CategorySidebarComponent — category tree
    ├── search-facets/
    │   └── search-facets.ts      # SearchFacetsComponent — brand, price, rating filters
    └── pagination/
        └── pagination.ts         # PaginationComponent
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
| `loading` | `boolean` | Product loading state |
| `error` | `string \| null` | Error message |
| `stockQuantity` | `number \| null` | Selected SKU stock level |
| `stockLoading` | `boolean` | Stock check loading state |
| `variantMatrix` | `VariantMatrix \| null` | Variant axis/option grid |
| `variantMatrixLoading` | `boolean` | Variant matrix loading state |
| `selectedVariants` | `Record<string, string>` | Selected axis values (e.g. `{ Color: "Black", Storage: "128GB" }`) |
| `productGallery` | `GalleryItem[]` | Product-level images |
| `skuGallery` | `GalleryItem[]` | Selected SKU images (overrides product gallery) |
| `galleryLoading` | `boolean` | Gallery loading state |

**Computed signals:** `hasVariantPicker`, `selectedVariantSku`, `mergedGallery`, `specEntries`

**Key methods:** `loadProduct(id)`, `loadStock(sku)`, `loadStoreInfo(storeId)`, `loadVariantMatrix(productId)`, `selectVariant(axisKey, value)`, `loadProductGallery(productId)`, `loadSkuGallery(skuId)`

## Key Routes

| Path | Component | Guard |
|:---|:---|:---|
| `/catalog` | `ProductListComponent` | None (public) |
| `/catalog/:id` | `ProductDetailComponent` | None (public) |

## Models (`catalog.models.ts`)

| Export | Kind | Description |
|:---|:---|:---|
| `Sku` | interface | SKU with id, skuCode, price, typedAttributes, flexibleAttributes |
| `Product` | interface | Full product with skus, gallery, tags |
| `ProductListItem` | interface | Lightweight grid item (no description, no skus array) |
| `PagedResult<T>` | interface | Generic paged response (items, totalCount, page, pageSize, totalPages) |
| `Category` | interface | Category with slug, parentCategoryId, sortOrder |
| `ProductStatus` | type | `'Draft' \| 'Active' \| 'Inactive' \| 'Deleted'` |
| `VariantAxis` | interface | Single variant axis (key, displayName, values) |
| `VariantOption` | interface | Single SKU combination with availability |
| `VariantMatrix` | interface | Product variant grid (axes + options) |
| `GalleryItem` | interface | Image/video from Media.API |
| `SearchResult<T>` | interface | Search response with facets |
| `FacetValue` | interface | Facet key + count |
| `ProductListParams` | interface | Query params for catalog list endpoint |
| `ProductSearchParams` | interface | Query params for search endpoint |

## Test Coverage Status

| Spec File | Status |
|:---|:---|
| `components/buy-box/buy-box.spec.ts` | ✅ Passing |
| `components/product-card/product-card.spec.ts` | ✅ Passing |
| `product-detail/product-detail.spec.ts` | ✅ Passing |
| `product-detail/product-detail.store.spec.ts` | ✅ Passing |
| `catalog.store.ts` | ❌ **No tests** |
| `catalog.service.ts` | ❌ **No tests** |

**E2E Coverage:** `browse-products.spec.ts`, `catalog-filter-sort.spec.ts`. Missing: product detail, variant picker, image gallery, search facets, API failure states.

## Known Gaps / Issues

- **Store/service tests missing:** `CatalogStore`, `CatalogService` have 0 unit tests.
- **No sort option in CatalogStore:** Product list sorting relies entirely on API defaults.
- **`isSearchMode` computation:** Dual-API routing (Catalog.API vs Search.API) is implicit — `refresh()` checks `searchQuery` length, but other filter changes don't always trigger the right API.
- **Category sidebar tree:** Uses `CategoryTreeService` from core — no deep-linking or URL-based category selection.
- **Variant picker:** No error state UI when variant matrix fails to load.
- **Gallery:** No fallback when both product and SKU galleries are empty.

---

*Last Updated: 2026-06-20*
