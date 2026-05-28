# Hybrid Media Caching Architecture — Implementation Plan

## Goal
Implement the hybrid pattern: Catalog/Search cache `PrimaryImageUrl` for list views (no N+1), BFF fetches full gallery from Media.API for detail pages.

## Current State Analysis

### ✅ Already Working
| Component | Status | Details |
|-----------|--------|---------|
| `Sku.ImageUrl` | ✅ Exists | Domain entity has `SetImageUrl()` |
| `Product.ImageUrl` | ✅ Exists | Domain entity has `SetImageUrl()` |
| `MediaUploadedConsumer` (Catalog) | ✅ Working | Updates Product/SKU ImageUrl on primary upload |
| `GalleryUpdatedConsumer` (Catalog) | ✅ Working | Updates Product/SKU ImageUrl on gallery reorder/primary change |
| `MediaDeletedConsumer` (Catalog) | ✅ Working | Clears ImageUrl with WasPrimary check |
| `ProductSearchDocument.ImageUrl` | ✅ Exists | Cached in Elasticsearch |
| `ProductUpdatedConsumer` (Search) | ✅ Working | Syncs ImageUrl from Catalog domain events |
| `ProductBffService` | ✅ Working | Fetches product + gallery for PDP |

### 🔧 Gaps to Fix

| # | Issue | Severity | Impact |
|---|-------|----------|--------|
| 1 | BFF fetches catalog + media **sequentially** | 🟡 Performance | PDP latency = sum of both calls |
| 2 | No BFF endpoint for SKU detail page | 🟡 Missing | Frontend can't get SKU + gallery in one call |
| 3 | Search.API doesn't listen to Media events | 🟡 Sync gap | ImageUrl stale until Catalog domain event fires |
| 4 | Media URLs are relative (`/api/media/{id}`) | 🟢 Design | BFF resolves — acceptable, document the pattern |

## Phases

### Phase 1: BFF Parallel Fetches
**File:** `src/Gateways/ApiGateway/Services/ProductBffService.cs`

Change `GetProductWithGalleryAsync` to use `Task.WhenAll` for parallel catalog + media fetches.

### Phase 2: SKU Detail BFF Endpoint
**Files:** `ProductBffService.cs`, `BffEndpoints.cs`

Add `GetSkuWithGalleryAsync` method + `/bff/catalog/skus/{skuId}` endpoint that returns SKU data + gallery in one call.

### Phase 3: Search Media Event Consumer
**New file:** `src/Microservices/Search/Search.API/Consumers/MediaGalleryUpdatedConsumer.cs`

Listen to `GalleryUpdatedIntegrationEvent` and update `ProductSearchDocument.ImageUrl` in Elasticsearch.

### Phase 4: Verification
- Build all projects
- Verify integration event flow end-to-end

## Files to Modify
- `src/Gateways/ApiGateway/Services/ProductBffService.cs` — parallel fetches + SKU detail
- `src/Gateways/ApiGateway/Endpoints/BffEndpoints.cs` — new SKU endpoint
- `src/Microservices/Search/Search.API/Consumers/MediaGalleryUpdatedConsumer.cs` — NEW
- `src/Microservices/Search/Search.API/Program.cs` — register consumer

## Status
in_progress
