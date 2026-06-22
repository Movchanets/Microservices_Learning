# Search Service

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Thin (API-only, no Domain/Application layers) |
| **Database** | Elasticsearch |
| **Messaging** | RabbitMQ via MassTransit (consumers only) |
| **Project Path** | `src/Microservices/Search/` |

## Key Models

| Model | Key Fields |
|:---|:---|
| `ProductSearchDocument` | Id, Name, Description, **MinPrice**, **MaxPrice**, Currency, **SkuCount**, CategoryId, CategoryName, Tags, **ImageUrl** (cached), StoreId, IsActive, Brand, Attributes (dict), **VariantAxes** (dict of lists), Rating, ReviewCount, InStock, CreatedAt, UpdatedAt |
| `SearchResult<T>` | Items, TotalCount, Page, PageSize, Facets, TotalPages |
| `FacetValue` | Key, Count |

### SKU Refactor Status

Search index is **product-level** with aggregated SKU data:
- `MinPrice` / `MaxPrice` — range across all active SKUs
- `SkuCount` — total number of active SKUs
- `VariantAxes` — dictionary of variant-axis values for faceted search (e.g., `{ "color": ["Black","White"], "storage": ["128GB","256GB"] }`)
- `Attributes` — merged product-level attributes

**VariantAxes** enables faceted filtering by SKU attributes (color, size, storage) without nested documents. Each variant axis aggregates all unique values from the product's active SKUs.

## API Endpoints (`/api/search`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/products` | `ISearchService.SearchAsync()` | Public |

### Query Parameters

| Param | Type | Description |
|:---|:---|:---|
| `q` | string | Full-text search query |
| `categoryId` | Guid | Filter by category |
| `priceMin` | decimal | Minimum price |
| `priceMax` | decimal | Maximum price |
| `tags` | string | Comma-separated tag filter |
| `brand` | string | Brand filter |
| `minRating` | double | Minimum rating |
| `inStock` | bool | In-stock filter |
| `page` | int | Page number (default 1) |
| `pageSize` | int | Results per page (default 20, max 100) |

## Integration Events

### Consumed (from Catalog.API)

| Event | Consumer | Action |
|:---|:---|:---|
| `ProductCreatedEvent` | `ProductCreatedConsumer` | Indexes new product in search (price/SKU data arrives later via SkuCreated) |
| `SkuCreatedIntegrationEvent` | `SkuCreatedConsumer` | Updates product in search (adds SKU data, updates price range) |
| `SkuDeletedEvent` | `SkuDeletedConsumer` | Updates product index (removes SKU data) |
| `SkuPriceChangedEvent` | `SkuPriceChangedConsumer` | Updates price range in search index |
| `ProductDeletedEvent` | `ProductDeletedConsumer` | Removes product from index |
| `ProductUpdatedEvent` | `ProductUpdatedConsumer` | Re-indexes product (name, description, tags, ImageUrl) |

### Consumed (from Media.API)

| Event | Consumer | Action |
|:---|:---|:---|
| `MediaUploadedIntegrationEvent` | `MediaUploadedConsumer` | Updates ProductSearchDocument.ImageUrl on primary image upload (handles both Product and SKU targets with LinkedProductId) |
| `GalleryUpdatedIntegrationEvent` | `MediaGalleryUpdatedConsumer` | Updates ProductSearchDocument.ImageUrl when primary image changes via gallery reorder/set-primary |

The `MediaUploadedConsumer` handles the initial upload path, while `MediaGalleryUpdatedConsumer` handles gallery reorder/set-primary events. Both keep Elasticsearch in sync directly from Media events without waiting for Catalog domain events to propagate.

## Current Status

- ✅ Full-text search with faceted filtering
- ✅ SKU-level consumers (SkuCreated, SkuDeleted, SkuPriceChanged)
- ✅ ProductCreated consumer for initial indexing
- ✅ Media consumers for ImageUrl sync (upload + gallery update)
- ✅ VariantAxes for faceted search by SKU attributes
- ✅ Aggregated price range (MinPrice/MaxPrice) across SKUs
- 🟠 `InStock` field not updated on stock changes (no inventory event for AddStock)

---

*Last Updated: 2026-06-20*
