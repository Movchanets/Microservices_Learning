# Rozetka Scraper + Media Seeder Implementation Plan

## Status: `in_progress`

## Problem Statement

The Media.API is fully implemented (Azurite storage, thumbnails, gallery CRUD, integration events) but the seeder doesn't properly populate it with images. The products.json has Rozetka data with local image paths, but:

1. **TargetType case mismatch** — BFF uses `"SKU"`, MediaSeeder uses `"Sku"` → gallery lookups fail silently
2. **iPhone 17 Pro Max name too long** — 239 chars exceeds Catalog API's 200 char limit
3. **No Rozetka scraper tool** — need a standalone tool to fetch product data + images from Rozetka
4. **Only 2 Rozetka products** — need more variety for demo

## Architecture Analysis

### Current Flow
```
Rozetka URL → (manual) → products.json → Seeder.App → Catalog API (products + SKUs)
                                                  ↓
                                           Media API → Azurite (blobs)
```

### Target Flow
```
Rozetka URL → RozetkaScraper → products.json + Data/Images/
                                      ↓
                               Seeder.App → Catalog API (products + SKUs)
                                        → Media API → Azurite (gallery per SKU)
```

### Key Files
| File | Purpose |
|------|---------|
| `src/Tools/Seeder.App/Seeders/MediaSeeder.cs` | Uploads images to Media API |
| `src/Tools/Seeder.App/Seeders/ProductSeeder.cs` | Creates products + variant SKUs |
| `src/Tools/Seeder.App/Worker.cs` | Orchestrates seeding flow |
| `src/Tools/Seeder.App/Data/products.json` | Product seed data |
| `src/Tools/Seeder.App/Data/Images/` | Downloaded Rozetka images |
| `src/Microservices/Media/Media.API/Endpoints/MediaEndpoints.cs` | Upload endpoint |
| `src/Gateways/ApiGateway/Services/ProductBffService.cs` | BFF gallery aggregation |

## Phases

### Phase 1: Fix TargetType Case Mismatch
**Files:** `MediaSeeder.cs`, `ProductBffService.cs`

The BFF calls `/api/media/gallery/SKU/{skuId}` (uppercase), but MediaSeeder uploads with `targetType: "Sku"` (PascalCase). This means BFF gallery lookups return empty.

**Fix:** Standardize on `"SKU"` (uppercase) everywhere — it's what the BFF expects.

### Phase 2: Fix iPhone Name Length
**File:** `Data/products.json`

Trim the iPhone 17 Pro Max name from 239 chars to < 200 chars. Keep it descriptive but concise.

### Phase 3: Create Rozetka Scraper
**New file:** `src/Tools/RozetkaScraper/` (standalone .NET console app or Python script)

Scrapes Rozetka product pages to extract:
- Product name, description, price
- Variant SKUs (storage, color options)
- Gallery image URLs
- Category breadcrumbs

Downloads images to `Data/Images/{slug}/image{N}.jpg`.

### Phase 4: Update Seeder for SKU Galleries
**File:** `Worker.cs`, `MediaSeeder.cs`

Ensure the seeder:
1. Uploads product-level gallery (TargetType="Product")
2. Uploads per-SKU gallery for variants (TargetType="SKU")
3. Sets primary image correctly
4. Handles image download failures gracefully

### Phase 5: Add More Rozetka Products
**File:** `Data/products.json`

Add 3-5 more Rozetka products with real data and downloaded images.

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| iPhone name 239 chars > 200 limit | 1 | Trim name in products.json |
| TargetType "Sku" vs "SKU" mismatch | 1 | Standardize to "SKU" |
| Seeder logs show "Image not found" | 1 | Images exist but path resolution may differ at runtime — need to verify Data/Images/ is copied to output |

## Acceptance Criteria
- [ ] Rozetka scraper can fetch product data + images
- [ ] products.json has 5+ Rozetka products with variants
- [ ] Each product has gallery images in Data/Images/
- [ ] MediaSeeder uploads with correct TargetType ("SKU")
- [ ] BFF can fetch SKU gallery via `/api/media/gallery/SKU/{skuId}`
- [ ] All images stored in Azurite blob storage
- [ ] Seeder runs without errors
