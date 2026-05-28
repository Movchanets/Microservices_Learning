# Tomorrow's Plan — May 29, 2026

## Today's Session Summary (May 28)

### Completed
1. **Rozetka Scraper** — Python script that scrapes JSON-LD from Rozetka, downloads images, generates products.json entries
2. **Seeder Fixes** — TargetType "SKU" normalization, iPhone name fix, content type magic byte detection, CopyToOutputDirectory=Always
3. **Media API URL Fix** — Images served through `/api/media/{id}` instead of raw Azurite blob URLs
4. **Code Review** — 20 findings (3 blocking, 8 important, 5 nits, 4 suggestions)
5. **All Blocking Fixes** — Outbox order, blob URL leak, MediaDeletedConsumer WasPrimary race condition
6. **Important Fixes** — IMediaStorageService removed from app layer, BFF URL resolution, TargetType normalization, GalleryEntry exception types, AsNoTracking fix
7. **Hybrid Media Caching** — BFF parallel fetches (Task.WhenAll), SKU detail endpoint, Search media event consumer
8. **SKU-by-ID Endpoint** — `GET /api/catalog/products/skus/{skuId}` + GetSkuByIdQuery + handler

### Build Status
- Media.API ✅
- Catalog.API ✅
- ApiGateway ✅
- Search.API ✅
- Seeder.App ✅

---

## Tomorrow's Tasks

### Priority 1: Deploy & Verify (30 min)

- [ ] **Full AppHost restart** — stop all processes, restart Aspire
- [ ] **Verify Media API starts** — check health endpoint
- [ ] **Run seeder** — verify images upload to Azurite with correct URLs
- [ ] **Verify gallery URLs** — `GET /api/media/gallery/Product/{id}` returns `/api/media/{id}` URLs
- [ ] **Verify BFF** — `GET /bff/catalog/products/{id}` returns gallery with absolute URLs

### Priority 2: End-to-End Image Flow Test (30 min)

- [ ] Upload image via `POST /api/media/upload` — verify URL is `/api/media/{id}`
- [ ] Verify `MediaUploadedIntegrationEvent` fires with correct URL
- [ ] Verify `Catalog.API` consumer updates `Product.ImageUrl` / `Sku.ImageUrl`
- [ ] Verify `Search.API` consumer updates `ProductSearchDocument.ImageUrl`
- [ ] Verify `GalleryUpdatedIntegrationEvent` fires on reorder/primary change
- [ ] Verify `MediaDeletedIntegrationEvent` includes `WasPrimary` field
- [ ] Test image download via `GET /api/media/{id}` — returns binary stream

### Priority 3: Rozetka Scraper Enhancement (1 hour)

- [ ] **Scrape 3-5 more products** — diverse categories (headphones, laptop, phone case)
- [ ] **Test variant extraction** — verify accessory filtering works
- [ ] **Update products.json** — add scraped products with local image paths
- [ ] **Re-seed** — verify all products have galleries

### Priority 4: Remaining Review Nits (30 min)

- [ ] **Content type validation duplication** — extract shared constant between handler and validator
- [ ] **ImageProcessingService interface** — wrap in interface for testability
- [ ] **CreateIfNotExistsAsync** — move to lazy initialization (startup or first use)
- [ ] **GalleryEntry FK** — add foreign key from GalleryEntry.MediaItemId to MediaItem.Id with cascade delete (prevents orphaned entries)

### Priority 5: BFF SKU Detail Endpoint Testing (30 min)

- [ ] Test `GET /bff/catalog/skus/{skuId}` — returns SKU + gallery
- [ ] Verify parallel fetch works (check response time vs sequential)
- [ ] Test with non-existent SKU — should return 404
- [ ] Test with SKU that has no gallery — should return empty gallery array

### Priority 6: Search Consumer Testing (30 min)

- [ ] Verify `MediaGalleryUpdatedConsumer` is registered and consuming
- [ ] Upload image for a product — verify Elasticsearch `ImageUrl` updates
- [ ] Change primary image — verify Elasticsearch updates
- [ ] Delete primary image — verify Elasticsearch clears `ImageUrl`

---

## Architecture State

### Data Flow (Final)
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

### Key URLs
| Endpoint | Purpose |
|----------|---------|
| `GET /api/media/{id}` | Serve image binary |
| `GET /api/media/{id}/thumbnail` | Serve thumbnail |
| `GET /api/media/gallery/Product/{id}` | Product gallery |
| `GET /api/media/gallery/SKU/{id}` | SKU gallery |
| `POST /api/media/upload` | Upload image |
| `GET /bff/catalog/products/{id}` | Product + gallery (parallel) |
| `GET /bff/catalog/skus/{id}` | SKU + gallery (parallel) |
| `GET /bff/catalog/skus/{id}/gallery` | SKU gallery only |
| `GET /api/catalog/products/skus/{id}` | SKU by ID |

---

## Known Issues to Watch

1. **Aspire rebuild locks processes** — need to `taskkill` before rebuild if process is stuck
2. **Media API port changes** — check resource list after restart for new port
3. **Seeder images are WebP but named .jpg** — magic byte detection handles this
4. **Rozetka anti-bot** — use fresh browser context per product URL
5. **GalleryEntry TargetType** — normalized to UPPERCASE in Create(), consumers use OrdinalIgnoreCase
