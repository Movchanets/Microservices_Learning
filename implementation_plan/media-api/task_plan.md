# Media.API Implementation Plan

## Goal
Transform the Media.API stub into a fully functional microservice with gallery management, EF Core persistence, CQRS via MediatR, and MassTransit integration events — following the project's established patterns. Integrate gallery across Product/SKU models, BFF, and frontend.

## Current Phase
Phase 9 — Complete

## Phases

### Phase 1: Domain Layer
**Status:** `complete`

- [x] 1.1 Create `MediaType` enum (Image, Video)
- [x] 1.2 Create `MediaItem` entity (inherits Entity, NOT AggregateRoot)
- [x] 1.3 Create `GalleryEntry` entity (links media to targets)
- [x] 1.4 ~~Create domain events~~ — Removed: entities not AggregateRoot, events wouldn't dispatch. Integration events published directly from handlers instead.
- [x] 1.5 Create `IMediaRepository` interface
- [x] 1.6 Create `IGalleryRepository` interface

### Phase 2: Infrastructure Layer
**Status:** `complete`

- [x] 2.1 Create `MediaDbContext` extending `DomainEventsDbContext` + MassTransit Outbox
- [x] 2.2 Create `MediaItemConfiguration` (unique index on BlobName)
- [x] 2.3 Create `GalleryEntryConfiguration` (composite index on TargetId+TargetType)
- [x] 2.4 Create `MediaRepository`
- [x] 2.5 Create `GalleryRepository`
- [x] 2.6 Create `IMediaStorageService` interface
- [x] 2.7 Create `AzureBlobStorageService` (refactored from inline blob logic)
- [x] 2.8 Create `DependencyInjection.cs` (AddDbContext, NOT AddNpgsqlDbContext)
- [x] 2.9 Create `DatabaseMigrationExtensions.cs`

### Phase 3: Application Layer (CQRS)
**Status:** `complete`

- [x] 3.1 `UploadMediaCommand` + Handler + Validator (Stream-based, not IFormFile)
- [x] 3.2 `DeleteMediaCommand` + Handler + Validator
- [x] 3.3 `UpdateGalleryOrderCommand` + Handler + Validator
- [x] 3.4 `SetPrimaryMediaCommand` + Handler + Validator
- [x] 3.5 `GetGalleryQuery` + Handler
- [x] 3.6 `MediaItemDto` DTO
- [x] 3.7 `GalleryOrderItem` DTO

### Phase 4: API Layer
**Status:** `complete`

- [x] 4.1 Rewrite `MediaEndpoints.cs` — 7 endpoints, all using ISender
  - `POST /api/media/upload` (multipart/form-data)
  - `DELETE /api/media/{mediaId:guid}`
  - `GET /api/media/gallery/{targetType}/{targetId:guid}`
  - `PUT /api/media/gallery/{targetType}/{targetId:guid}/reorder`
  - `PUT /api/media/gallery/{targetType}/{targetId:guid}/primary/{mediaItemId:guid}`
  - `GET /api/media/{mediaId:guid}` (stream file)
  - `GET /api/media/{mediaId:guid}/thumbnail` (stream thumbnail)
- [x] 4.2 Rewrite `Program.cs` (Aspire, MediatR, MassTransit, FluentValidation, Auth)

### Phase 5: Integration Events
**Status:** `complete`

- [x] 5.1 Create `MediaUploadedIntegrationEvent` in SharedContracts
- [x] 5.2 Create `MediaDeletedIntegrationEvent` in SharedContracts
- [x] 5.3 Create `GalleryUpdatedIntegrationEvent` in SharedContracts
- [x] 5.4 Create `GalleryItemContract` in SharedContracts
- [x] 5.5-5.7 ~~Domain event handlers~~ — Skipped: integration events published directly from command handlers via IPublishEndpoint (thin service, no AggregateRoot)

### Phase 6: Aspire & Database
**Status:** `complete`

- [x] 6.1 Update `AppHost.cs` — added `mediaDb` database reference
- [x] 6.2 Add EF Core migration: `InitialCreate`
- [x] 6.3 Update `appsettings.json` — added connection string placeholder
- [x] 6.4 Update `Media.API.csproj` — added all NuGet packages

### Phase 7: Catalog Integration (Product/SKU models + consumers)
**Status:** `complete`

- [x] 7.1 Add `ImageUrl` property to `Sku` entity + `SetImageUrl()` method
- [x] 7.2 Update `SkuDto` to include `ImageUrl`
- [x] 7.3 Update `SkuConfiguration` (HasMaxLength(2000))
- [x] 7.4 Update all `SkuDto` constructors (ProductReadRepository, AddSkuHandler, UpdateProductHandler)
- [x] 7.5 Create `MediaUploadedConsumer` in Catalog (updates Product.ImageUrl / Sku.ImageUrl)
- [x] 7.6 Create `GalleryUpdatedConsumer` in Catalog (updates from primary gallery item)
- [x] 7.7 Create `MediaDeletedConsumer` in Catalog (clears ImageUrl)
- [x] 7.8 Register consumers in Catalog `Program.cs`
- [x] 7.9 Add EF Core migration: `AddSkuImageUrl`

### Phase 8: BFF & Frontend Integration
**Status:** `complete`

- [x] 8.1 Create `ProductBffService` in Gateway (enriches product with gallery from Media.API)
- [x] 8.2 Add BFF endpoints: `/bff/catalog/products/{id}`, `/bff/catalog/skus/{skuId}/gallery`
- [x] 8.3 Register `ProductBffService` in Gateway `Program.cs`
- [x] 8.4 Update frontend `catalog.models.ts` — `imageUrl` on Sku, `gallery` on Product, new `GalleryItem` interface
- [x] 8.5 Create `ImageGalleryComponent` (main image + thumbnails)
- [x] 8.6 Update product-detail page to use `<app-image-gallery>`
- [x] 8.7 Update `CatalogService.getProduct()` → `/bff/catalog/products/{id}` (with gallery)
- [x] 8.8 Create `MediaService` for frontend file upload/delete
- [x] 8.9 Update seller product form with file upload (replaces text input)

### Phase 9: Verification
**Status:** `complete`

- [x] 9.1 Full solution build: **0 errors**
- [x] 9.2 Frontend `ng build`: **0 errors**
- [x] 9.3 Unit tests: **262 passing** (1 pre-existing failure in Search.UnitTests)
- [x] 9.4 EF Core migrations created (InitialCreate + AddSkuImageUrl)
- [x] 9.5 YARP config verified — `/bff/*` handled locally, `/api/media/*` proxied

---

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| Single-project with folder layers | Media is designated "thin" — no need for 4 .csproj projects |
| Entities inherit Entity (not AggregateRoot) | No complex domain invariants — gallery is a simple CRUD concern |
| Integration events published directly from handlers | No domain events needed — thin service, IPublishEndpoint after SaveChanges |
| Normalized model (MediaItem + GalleryEntry) | User's requirement; future-proofs for shared media |
| Azure Blob Storage (keep existing) | Already configured in Aspire with Azurite emulator |
| AddDbContext (not AddNpgsqlDbContext) | Per memory: AddNpgsqlDbContext conflicts with EF Core 10 |
| KebabCaseEndpointNameFormatter() (no args) | Version 8.5.9 doesn't accept prefix parameter |
| Product.ImageUrl stays as denormalized cache | Fast for list views; synced via Media integration events |
| Sku gets ImageUrl property | Each SKU variant can have own gallery (e.g., red vs blue t-shirt) |
| BFF enriches product with gallery | Single call from frontend; gallery fetched from Media.API server-side |

---

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| CS0246: IUnitOfWork not found | 1 | Added `using BuildingBlocks.SharedContracts.Abstractions` |
| CS1061: AddInboxStateEntity not found | 1 | Added `using MassTransit` to DbContext |
| CS1501: SetKebabCaseEndpointNameFormatter 2 args | 1 | Changed to no-arg overload (v8.5.9) |
| CS1061: AddEntityFrameworkOutbox not found | 1 | Added `MassTransit.EntityFrameworkCore` package |
| CS0246: IMediaRepository not found in endpoints | 1 | Added `using Media.API.Domain` |
| TS2307: Cannot find module in media.service.ts | 1 | Fixed import path: `../../features/catalog/catalog.models` |
| Product.Update(null) doesn't clear ImageUrl | 1 | Added `Product.SetImageUrl()` method, updated all consumers |

---

## Notes
- Domain events were created then removed — MediaItem is not AggregateRoot, so DomainEventDispatcherInterceptor won't dispatch them. Integration events published directly from handlers.
- `ImageProcessingService` kept as-is — well-structured, generates thumbnails on upload.
- Search.UnitTests has 1 pre-existing failure (not related to Media changes).
- `WithOpenApi()` is deprecated in .NET 10 — cosmetic warning, still functional.
