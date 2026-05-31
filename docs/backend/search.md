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
| `ProductSearchDocument` | Id, Name, Description, Price, Currency, Sku, CategoryId, CategoryName, Tags, **ImageUrl** (cached), StoreId, IsActive, Brand, Attributes (dict), Rating, ReviewCount, InStock |
| `SearchResult<T>` | Items, TotalCount, Page, PageSize, Facets |
| `FacetValue` | Key, Count |

### SKU Refactor Status

Search index is **product-level** with single-valued fields:
- `Price` — single decimal (not per-SKU)
- `Sku` — single string
- `Attributes` — single dictionary
- `InStock` — single boolean

**Known limitation:** Multi-SKU products only show one SKU's data. Faceted search by SKU attributes (size, color) is not yet possible. Two options for fix:
- **Option A (nested):** Store SKUs as nested objects within product document
- **Option B (denormalized):** Index one document per SKU

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
| `SkuCreatedIntegrationEvent` | `SkuCreatedConsumer` | Indexes/updates product in search |
| `SkuDeletedEvent` | `SkuDeletedConsumer` | Updates product index (removes SKU data) |
| `SkuPriceChangedEvent` | `SkuPriceChangedConsumer` | Updates price in search index |
| `ProductDeletedEvent` | `ProductDeletedConsumer` | Removes product from index |
| `ProductUpdatedEvent` | `ProductUpdatedConsumer` | Re-indexes product (name, description, tags, ImageUrl) |

### Consumed (from Media.API)

| Event | Consumer | Action |
|:---|:---|:---|
| `GalleryUpdatedIntegrationEvent` | `MediaGalleryUpdatedConsumer` | Updates ProductSearchDocument.ImageUrl when primary image changes |

This is a **fast path** — the Catalog consumer handles the canonical Product.ImageUrl update, but this consumer keeps Elasticsearch in sync directly from Media events without waiting for the Catalog domain event to propagate.

## Current Status

- ✅ Full-text search with faceted filtering
- ✅ SKU-level consumers (SkuCreated, SkuDeleted, SkuPriceChanged)
- ✅ Media gallery consumer for ImageUrl sync
- 🟠 **Product-level index** — single Price/Sku/Attributes per product document
- 🟡 `InStock` field not updated on stock changes (no inventory event for AddStock)
