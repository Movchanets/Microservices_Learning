# Media Service

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Thin (single project with folder-based layers) |
| **Storage** | Azure Blob Storage (Azurite locally) |
| **Messaging** | MassTransit (RabbitMQ) with EF Outbox |
| **Project Path** | `src/Microservices/Media/Media.API/` |

## Architecture

Single project with folder-based layers (no separate .csproj — Media is "thin"):

```
Media.API/
├── Domain/          — MediaItem, GalleryEntry, MediaType enum, repository interfaces
├── Application/     — Commands (Upload, Delete, Reorder, SetPrimary), Query (GetGallery)
├── Infrastructure/  — MediaDbContext, repositories, AzureBlobStorageService, DI
├── Endpoints/       — 7 Minimal API endpoints
├── Services/        — ImageProcessingService (thumbnails)
└── Program.cs
```

## API Endpoints (`/api/media`)

| Method | Path | Auth | Description |
|:---|:---|:---:|:---|
| `POST` | `/upload` | Yes | Upload image/video (multipart/form-data) |
| `GET` | `/gallery/{targetType}/{targetId}` | No | Get gallery for target (Product/SKU) |
| `GET` | `/{mediaId}` | No | Serve file binary |
| `GET` | `/{mediaId}/thumbnail` | No | Serve thumbnail |
| `DELETE` | `/{mediaId}` | Yes | Delete media + blob |
| `PUT` | `/gallery/{targetType}/{targetId}/reorder` | Yes | Reorder gallery |
| `PUT` | `/gallery/{targetType}/{targetId}/primary/{mediaItemId}` | Yes | Set primary image |

## Domain Model

| Entity | Description |
|:---|:---|
| `MediaItem` | File metadata (FileName, ContentType, BlobName, Url, SizeBytes, Type, ThumbnailBlobName) |
| `GalleryEntry` | Links media to target (MediaItemId → TargetId/TargetType, SortOrder, IsPrimary) |
| `MediaType` | Enum: Image, Video |

**Key convention:** `TargetType` is normalized to UPPERCASE in `GalleryEntry.Create()`. All repository queries use `ToUpperInvariant()` for consistent matching.

## Integration Events

| Event | Trigger | Consumers |
|:---|:---|:---|
| `MediaUploadedIntegrationEvent` | Image uploaded with IsPrimary=true | Catalog (updates Product/SKU.ImageUrl) |
| `GalleryUpdatedIntegrationEvent` | Gallery reordered or primary changed | Catalog + Search |
| `MediaDeletedIntegrationEvent` | Image deleted | Catalog (clears ImageUrl if WasPrimary) |

## Storage Architecture

- **Container:** `media`
- **Naming:** `{guid}.{ext}` for originals, `thumb_{guid}.{ext}` for thumbnails
- **Allowed types:** image/jpeg, image/png, image/gif, image/webp, video/mp4
- **Max size:** 10 MB (images), 100 MB (video)

## BFF Integration

The Gateway's `ProductBffService` fetches gallery from Media.API in parallel with catalog data:

| BFF Endpoint | Description |
|:---|:---|
| `GET /bff/catalog/products/{id}` | Product + gallery (parallel fetch) |
| `GET /bff/catalog/skus/{skuId}` | SKU + gallery (parallel fetch) |
| `GET /bff/catalog/skus/{skuId}/gallery` | SKU gallery only |

## Key Files

| File | Purpose |
|:---|:---|
| `Endpoints/MediaEndpoints.cs` | 7 Minimal API endpoints |
| `Domain/Entities/MediaItem.cs` | File metadata entity |
| `Domain/Entities/GalleryEntry.cs` | Links media to targets |
| `Application/Commands/UploadMedia/UploadMediaHandler.cs` | Upload + thumbnail + gallery entry |
| `Application/MediaUrlExtensions.cs` | URL building (relative, not blob URLs) |
| `Infrastructure/Repositories/GalleryRepository.cs` | Gallery queries with TargetType normalization |

## Current Status

- ✅ Full CRUD with thumbnail support
- ✅ Gallery management (reorder, set primary)
- ✅ Integration events (MediaUploaded, GalleryUpdated, MediaDeleted)
- ✅ BFF parallel fetch for product detail pages
- ✅ Content-type validation and size limits
- ⚠️ No virus scanning or content moderation
