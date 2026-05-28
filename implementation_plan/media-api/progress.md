# Media.API — Progress Log

## Session 1: Media.API Core Implementation
**Date:** 2026-05-27
**Goal:** Implement Media.API microservice from stub

### What Was Done
1. Created Domain Layer: MediaType enum, MediaItem entity, GalleryEntry entity, repository interfaces
2. Created Infrastructure Layer: MediaDbContext, EF configs, repositories, AzureBlobStorageService, DI
3. Created Application Layer: 4 commands + 1 query with handlers and validators, DTOs
4. Created API Layer: 7 endpoints, rewrote Program.cs
5. Created Integration Events in SharedContracts (4 events)
6. Updated Aspire AppHost (mediaDb)
7. Updated csproj (6 new packages)
8. Cleaned up dead code (removed unused domain events, old MediaUploadResponse)
9. Created EF Core migration: InitialCreate

### Build Result
- Media.API: **0 errors**
- Full solution: **0 errors**

---

## Session 2: Cross-Service Integration (Product/SKU + BFF + Frontend)
**Date:** 2026-05-27
**Goal:** Integrate Media gallery across Catalog, Gateway, and Angular frontend

### What Was Done

#### Catalog.Domain
- Added `ImageUrl` property to `Sku` entity
- Added `SetImageUrl()` method to `Sku`
- Updated `SkuDto` to include `ImageUrl`
- Updated `SkuConfiguration` (HasMaxLength(2000))
- Updated all `SkuDto` constructors in ProductReadRepository, AddSkuHandler, UpdateProductHandler

#### Catalog.Infrastructure
- Created `MediaUploadedConsumer` — updates Product.ImageUrl / Sku.ImageUrl on primary upload
- Created `GalleryUpdatedConsumer` — updates ImageUrl from primary gallery item
- Created `MediaDeletedConsumer` — clears ImageUrl on media deletion

#### Catalog.API
- Registered 3 Media consumers in MassTransit configuration
- Created EF Core migration: AddSkuImageUrl

#### Gateway (BFF)
- Created `ProductBffService` — enriches product with gallery from Media.API
- Added `/bff/catalog/products/{id}` endpoint (product + gallery)
- Added `/bff/catalog/skus/{skuId}/gallery` endpoint
- Registered `ProductBffService` in Program.cs

#### Frontend
- Updated `catalog.models.ts`: `imageUrl` on Sku, `gallery` on Product, new `GalleryItem` interface
- Created `ImageGalleryComponent` — main image + clickable thumbnails
- Updated product-detail page to use `<app-image-gallery>`
- Updated `CatalogService.getProduct()` → `/bff/catalog/products/{id}` (with gallery)
- Created `MediaService` — file upload/delete/primary via Media.API
- Updated seller product form with file upload (replaces text input)

### Build Result
- Full solution: **0 errors**
- Frontend `ng build`: **0 errors**
- Unit tests: **262 passing** (1 pre-existing failure in Search.UnitTests)

### Files Created (Session 2)
```
Catalog.Infrastructure/Messaging/Consumers/MediaUploadedConsumer.cs
Catalog.Infrastructure/Messaging/Consumers/GalleryUpdatedConsumer.cs
Catalog.Infrastructure/Messaging/Consumers/MediaDeletedConsumer.cs
Catalog.Infrastructure/Migrations/20260526225123_AddSkuImageUrl.cs
Catalog.Infrastructure/Migrations/20260526225123_AddSkuImageUrl.Designer.cs
Gateway/Services/ProductBffService.cs
web/src/app/core/services/media.service.ts
web/src/app/features/catalog/components/image-gallery/image-gallery.ts
```

### Files Modified (Session 2)
```
Catalog.Domain/Entities/Sku.cs (added ImageUrl + SetImageUrl)
Catalog.Application/DTOs/DTOs.cs (added ImageUrl to SkuDto)
Catalog.Application/Commands/AddSku/AddSkuHandler.cs (SkuDto constructor)
Catalog.Application/Commands/UpdateProduct/UpdateProductHandler.cs (SkuDto constructor)
Catalog.Infrastructure/Persistence/SkuConfiguration.cs (ImageUrl config)
Catalog.Infrastructure/Repositories/ProductReadRepository.cs (SkuDto constructor)
Catalog.API/Program.cs (registered consumers)
Gateway/Endpoints/BffEndpoints.cs (added product gallery endpoints)
Gateway/Program.cs (registered ProductBffService)
web/src/app/features/catalog/catalog.models.ts (ImageUrl, GalleryItem)
web/src/app/features/catalog/catalog.service.ts (BFF endpoint)
web/src/app/features/catalog/product-detail/product-detail.ts (gallery component)
web/src/app/features/catalog/product-detail/product-detail.html (gallery template)
web/src/app/features/seller-dashboard/product-form/product-form.ts (file upload)
web/src/app/features/seller-dashboard/product-form/product-form.html (upload UI)
```

---

## Remaining Work
- [ ] End-to-end testing via Aspire dashboard

---

## Session 4: Bug Fixes & Verification
**Date:** 2026-05-27

### Bug Fix: Product.ImageUrl not clearing on media delete
- **Root cause:** `Product.Update(..., null)` uses `ImageUrl = imageUrl ?? ImageUrl` (null-coalescing), so null doesn't clear the value
- **Fix:** Added `Product.SetImageUrl(string? imageUrl)` method (like Sku already had)
- **Updated:** `MediaDeletedConsumer`, `MediaUploadedConsumer`, `GalleryUpdatedConsumer` — all use `SetImageUrl` now
- **Test fix:** Updated `MediaDeletedConsumerTests.Consume_ProductWithImageUrl_ClearsImageUrl` to expect null

### YARP Multipart Verification
- YARP proxies multipart/form-data by default — no special config needed
- Default 30MB request body limit is fine for images (10MB max)
- For video uploads (100MB), would need `MaxRequestBodySize` increase on Gateway
- `DisableAntiforgery()` already set on upload endpoint

---

## Session 3: Unit Tests
**Date:** 2026-05-27
**Goal:** Create unit tests for Media handlers and Catalog consumers

### What Was Done

#### Media.UnitTests (NEW — 11 tests)
- Created `tests/UnitTests/Media.UnitTests/` project
- `UploadMediaHandlerTests` — 3 tests (valid upload, invalid content type, file too large)
- `DeleteMediaHandlerTests` — 2 tests (existing media, not found)
- `GetGalleryHandlerTests` — 2 tests (with entries, empty)
- `SetPrimaryMediaHandlerTests` — 2 tests (valid, not in gallery)
- `UpdateGalleryOrderHandlerTests` — 2 tests (valid, no entries)

#### Catalog.UnitTests (7 new consumer tests)
- `MediaUploadedConsumerTests` — 3 tests (primary product, primary SKU, non-primary)
- `GalleryUpdatedConsumerTests` — 2 tests (with primary, no primary)
- `MediaDeletedConsumerTests` — 2 tests (product, SKU)

### Files Created
```
tests/UnitTests/Media.UnitTests/Media.UnitTests.csproj
tests/UnitTests/Media.UnitTests/Application/UploadMediaHandlerTests.cs
tests/UnitTests/Media.UnitTests/Application/DeleteMediaHandlerTests.cs
tests/UnitTests/Media.UnitTests/Application/GetGalleryHandlerTests.cs
tests/UnitTests/Media.UnitTests/Application/SetPrimaryMediaHandlerTests.cs
tests/UnitTests/Media.UnitTests/Application/UpdateGalleryOrderHandlerTests.cs
tests/UnitTests/Catalog.UnitTests/Infrastructure/MediaUploadedConsumerTests.cs
tests/UnitTests/Catalog.UnitTests/Infrastructure/GalleryUpdatedConsumerTests.cs
tests/UnitTests/Catalog.UnitTests/Infrastructure/MediaDeletedConsumerTests.cs
```

### Files Modified
```
tests/UnitTests/Catalog.UnitTests/Catalog.UnitTests.csproj (added Infrastructure ref + MassTransit + Sqlite)
```

### Final Test Results (all 11 projects)
| Project | Passed | Failed | Total |
|---------|--------|--------|-------|
| ApiGateway.UnitTests | 7 | 0 | 7 |
| BuildingBlocks.Infrastructure.UnitTests | 16 | 0 | 16 |
| Cart.UnitTests | 31 | 0 | 31 |
| Catalog.UnitTests | 30 | 0 | 30 |
| Identity.UnitTests | 45 | 0 | 45 |
| Inventory.UnitTests | 8 | 0 | 8 |
| **Media.UnitTests** | **11** | **0** | **11** |
| Notification.UnitTests | 20 | 0 | 20 |
| Ordering.UnitTests | 69 | 0 | 69 |
| Payment.UnitTests | 30 | 0 | 30 |
| Search.UnitTests | 3 | **1** | 4 |
| StoreManagement.UnitTests | 29 | 0 | 29 |
| **Total** | **299** | **1** | **300** |

Search.UnitTests failure is pre-existing (not related to Media changes).
