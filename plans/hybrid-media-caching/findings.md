# Hybrid Media Caching Architecture — Findings

## Architecture Assessment

The hybrid pattern (cached PrimaryImageUrl + dynamic gallery fetch) was **already 90% implemented**.

### What Already Existed
- `Sku.ImageUrl` + `Product.ImageUrl` — cached primary image in Catalog domain
- `MediaUploadedConsumer` — updates Catalog ImageUrl on primary upload
- `GalleryUpdatedConsumer` — updates Catalog ImageUrl on gallery reorder
- `MediaDeletedConsumer` — clears ImageUrl with WasPrimary check
- `ProductSearchDocument.ImageUrl` — cached in Elasticsearch
- `ProductUpdatedConsumer` in Search — syncs ImageUrl from Catalog domain events
- `ProductBffService` — fetches product + gallery for PDP

### Gaps Fixed

| Gap | Fix | Files |
|-----|-----|-------|
| BFF sequential fetches | `Task.WhenAll` parallel pattern | `ProductBffService.cs` |
| No SKU detail endpoint | Added `GetSkuByIdQuery` + handler + endpoint | 4 files |
| Search doesn't listen to Media events | Added `MediaGalleryUpdatedConsumer` | 3 files |
| BFF wrong SKU URL | Fixed to `/api/catalog/products/skus/{skuId}` | `ProductBffService.cs` |

### Data Flow (Final)

```
List Views (Search/Grid):
  Catalog.DB → Product.ImageUrl (cached) → Frontend
  Search.DB  → ProductSearchDocument.ImageUrl (cached) → Frontend
  No Media.API call needed = no N+1

Detail Page (PDP):
  BFF → Task.WhenAll(catalog, media) → merged JSON → Frontend
  BFF resolves relative /api/media/{id} → absolute URL via media-api BaseAddress

Image Upload Flow:
  Admin → Media.API → stores blob + creates GalleryEntry
                     → publishes MediaUploadedIntegrationEvent
                     → Catalog consumer updates Product/SKU.ImageUrl
                     → Search consumer updates ProductSearchDocument.ImageUrl
```
