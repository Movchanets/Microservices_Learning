# Media Service

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Thin (API-only, no Domain/Application layers) |
| **Storage** | Azure Blob Storage (Azurite locally) |
| **Messaging** | None |
| **Project Path** | `src/Microservices/Media/` |

## Storage Architecture

- **Container:** `media`
- **Naming:** `{guid}.{ext}` for originals, `thumb_{guid}.{ext}` for thumbnails
- **Allowed types:** image/jpeg, image/png, image/gif, image/webp, application/pdf
- **Max size:** 10 MB

## API Endpoints (`/api/media`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `POST` | `/upload` | Upload (multipart/form-data) | Authenticated |
| `GET` | `/{blobName}` | Download/serve file | Public |
| `GET` | `/{blobName}/thumbnail` | Serve thumbnail | Public |
| `GET` | `/` | List all files | Authenticated |
| `DELETE` | `/{blobName}` | Delete file + thumbnail | Authenticated |

### Upload Response

```csharp
record MediaUploadResponse(string BlobName, string Url, string ContentType, long Size);
```

## Features

- **Automatic thumbnail generation** for image uploads (via `ImageProcessingService`)
- **Antiforgery disabled** on upload endpoint (required for multipart/form-data)
- **Thumbnail cleanup** on delete (removes both original and `thumb_` prefixed thumbnail)

## Integration Events

None — Media is a standalone service with no event publishing or consumption.

## Current Status & Known Issues

- ✅ Full CRUD with thumbnail support
- ✅ Content-type validation and size limits
- ⚠️ No access control on file retrieval (anyone with blob name can download)
- ⚠️ No virus scanning or content moderation
