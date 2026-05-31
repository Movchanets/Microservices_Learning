# Media.API — Architecture Reference

## Overview

Media.API is a thin microservice for gallery management: upload images, link them to products/SKUs, manage gallery order, and serve files. Uses Azure Blob Storage (Azurite locally), EF Core, MediatR CQRS, and MassTransit integration events.

## Architecture

Single project with folder-based layers (no separate .csproj per layer — Media is designated "thin"):

```
Media.API/
├── Domain/          — Entities (MediaItem, GalleryEntry), Enums (MediaType), Interfaces
├── Application/     — Commands, Queries, Handlers, Validators, DTOs
├── Infrastructure/  — EF Core (MediaDbContext), Repositories, Blob Storage, DI
├── Endpoints/       — Minimal API endpoints
├── Services/        — ImageProcessingService (thumbnails)
└── Program.cs
```

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| Entities inherit `Entity` (not `AggregateRoot`) | No complex domain invariants — gallery is simple CRUD |
| Integration events published directly from handlers | No domain events needed — `IPublishEndpoint` after `SaveChanges` via Outbox |
| `AddDbContext` (not `AddNpgsqlDbContext`) | EF Core 10 conflict with `AddDbContextPool` |
| `Product.ImageUrl` stays as denormalized cache | Fast for list views; synced via Media integration events |
| `Sku.ImageUrl` added | Each variant can have own images (color, size) |
| BFF enriches product with gallery | Single call from frontend; gallery fetched from Media.API server-side |

## Data Flow

```
Image Upload:
  Admin → Media.API → blob storage + GalleryEntry
                     → MediaUploadedIntegrationEvent → Catalog.ImageUrl update
                                                     → Search.ImageUrl update

Gallery Change:
  Media.API → GalleryUpdatedIntegrationEvent → Catalog.ImageUrl update
                                              → Search.ImageUrl update

Image Delete:
  Media.API → MediaDeletedIntegrationEvent (WasPrimary) → Catalog.ImageUrl clear (if primary)

List Views:
  Catalog.DB → Product.ImageUrl (cached) → Frontend
  Search.DB  → ProductSearchDocument.ImageUrl (cached) → Frontend

Detail Page:
  BFF → Task.WhenAll(catalog, media) → merged JSON with absolute URLs → Frontend
```

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/media/upload` | Yes | Upload image/video (multipart/form-data) |
| `GET` | `/api/media/gallery/{targetType}/{targetId}` | No | Get gallery for target |
| `GET` | `/api/media/{mediaId}` | No | Serve file binary |
| `GET` | `/api/media/{mediaId}/thumbnail` | No | Serve thumbnail |
| `DELETE` | `/api/media/{mediaId}` | Yes | Delete media + blob |
| `PUT` | `/api/media/gallery/{targetType}/{targetId}/reorder` | Yes | Reorder gallery |
| `PUT` | `/api/media/gallery/{targetType}/{targetId}/primary/{mediaItemId}` | Yes | Set primary image |

## BFF Endpoints (Gateway)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/bff/catalog/products/{id}` | Product + gallery (parallel fetch) |
| `GET` | `/bff/catalog/skus/{skuId}` | SKU + gallery (parallel fetch) |
| `GET` | `/bff/catalog/skus/{skuId}/gallery` | SKU gallery only (lightweight) |

## Integration Events

| Event | Trigger | Consumers |
|-------|---------|-----------|
| `MediaUploadedIntegrationEvent` | Image uploaded with IsPrimary=true | Catalog (updates Product/SKU.ImageUrl) |
| `GalleryUpdatedIntegrationEvent` | Gallery reordered or primary changed | Catalog + Search |
| `MediaDeletedIntegrationEvent` | Image deleted | Catalog (clears ImageUrl if WasPrimary) |

## Catalog Consumers

Three consumers in `Catalog.Infrastructure/Messaging/Consumers/`:

| Consumer | Event | Action |
|----------|-------|--------|
| `MediaUploadedConsumer` | `MediaUploadedIntegrationEvent` | Updates Product/SKU ImageUrl when IsPrimary=true |
| `GalleryUpdatedConsumer` | `GalleryUpdatedIntegrationEvent` | Updates ImageUrl from primary gallery item |
| `MediaDeletedConsumer` | `MediaDeletedIntegrationEvent` | Clears ImageUrl (GalleryUpdatedConsumer will re-set if new primary) |

## Key Files

| File | Purpose |
|------|---------|
| `Media.API/Endpoints/MediaEndpoints.cs` | 7 Minimal API endpoints |
| `Media.API/Domain/Entities/MediaItem.cs` | File metadata entity |
| `Media.API/Domain/Entities/GalleryEntry.cs` | Links media to targets (Product/SKU) |
| `Media.API/Application/Commands/UploadMedia/UploadMediaHandler.cs` | Upload + thumbnail + gallery entry |
| `Media.API/Infrastructure/Repositories/GalleryRepository.cs` | Gallery queries with TargetType normalization |
| `Gateway/Services/ProductBffService.cs` | BFF parallel fetch + gallery merge |
| `Catalog.Infrastructure/Messaging/Consumers/` | 3 consumers for Media events |
| `Search.API/Consumers/MediaGalleryUpdatedConsumer.cs` | Search ImageUrl sync |

## Notes

- `TargetType` is normalized to UPPERCASE in `GalleryEntry.Create()` — repository queries use `ToUpperInvariant()` for consistency
- Media URLs are relative (`/api/media/{id}`) — BFF resolves to absolute URLs via media-api BaseAddress
- `WithOpenApi()` is deprecated in .NET 10 — cosmetic warning, still functional
- Search.UnitTests has 1 pre-existing failure (not related to Media changes)
