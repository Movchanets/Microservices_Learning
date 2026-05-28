# Findings: Rozetka Scraper + Media Seeder

## Architecture Analysis

### Media API (Fully Implemented)
- **Storage:** Azure Blob Storage via Azurite (local emulator)
- **Domain:** `MediaItem` + `GalleryEntry` (many-to-many via TargetId/TargetType)
- **Endpoints:** Upload, Delete, GetGallery, Reorder, SetPrimary, GetFile, GetThumbnail
- **Thumbnails:** Auto-generated for images via `ImageProcessingService`
- **Events:** `MediaUploadedIntegrationEvent`, `MediaDeletedIntegrationEvent`

### Seeder Architecture
- **Entry:** `Worker.cs` orchestrates: Users → Stores → Categories → Products → Inventory → Media → Orders
- **MediaSeeder:** Already supports both URL download and local file paths
- **ProductSeeder:** Creates products + variant SKUs from `Variants[]` in products.json
- **Data:** `Data/Images/` has 7 subdirectories with real Rozetka images (WebP format)

### BFF Gallery Integration
- `ProductBffService.GetProductWithGalleryAsync()` — merges product + gallery
- `ProductBffService.GetSkuGalleryAsync()` — fetches SKU-level gallery
- **Critical:** BFF uses `TargetType: "SKU"` (uppercase), seeder uses `"Sku"` (PascalCase)

## Issues Found

### 1. TargetType Case Mismatch (CRITICAL)
**Location:** `MediaSeeder.cs:32` uses `targetType = "Product"`, `MediaSeeder.cs:157` uses `targetType = "Sku"`
**BFF expects:** `"SKU"` (uppercase) — see `ProductBffService.cs:78`
**Impact:** SKU gallery lookups from BFF return empty arrays
**Fix:** Change MediaSeeder to use `"SKU"` for variant uploads

### 2. iPhone Name Too Long (BUG)
**Location:** `products.json` line 274
**Current:** 239 chars (exceeds Catalog API's 200 char limit)
**Fix:** Trim to ~190 chars

### 3. Image Format Mismatch
**Location:** `Data/Images/` — files are WebP but named `.jpg`
**MediaSeeder:** Sets `ContentType: "image/jpeg"` for all files
**Impact:** Media API accepts it (content type is stored but not validated against extension)
**Note:** The `file` CLI confirmed: `RIFF (little-endian) data, Web/P image` — they're WebP files with .jpg extension

### 4. Variant SKU Image URLs Not Set
**Location:** `ProductSeeder.cs:80` — `CreateSkuAsync()` doesn't pass `imageUrl`
**Impact:** SKU.ImageUrl is null even though gallery images exist
**Fix:** After creating variant SKU, set its ImageUrl to the primary gallery image

## Rozetka Page Structure (for scraper)
- Product URL: `https://rozetka.com.ua/ua/{slug}/{id}/`
- Images: In `img` tags within `.product-gallery` section
- Variants: In `.product-variations` section (color, storage options)
- Price: In `.product-prices__big` element
- Breadcrumbs: In `.breadcrumb` navigation

## Recommended Approach for Scraper
Since we're on Windows with Python available, a Python script using `requests` + `beautifulsoup4` is the simplest approach. Alternatively, a .NET console app using `HttpClient` + `AngleSharp`.

**Decision:** Python script — simpler for scraping, can be run independently, no build step needed.

## File Format Analysis
```
Data/Images/
├── 1/                    → iPhone 17 Pro 1TB variant (10 images)
├── 2/                    → iPhone 17 Pro 2TB variant (10 images)
├── 256/                  → iPhone 17 Pro 256GB variant (10 images)
├── 69-oled-.../          → iPhone 17 Pro Max main (10 images)
├── acer-nitro-lite-.../  → Acer laptop (10 images)
└── iphone-17-pro/        → iPhone 17 Pro variant (10 images)
```

All images are WebP format (~58KB each), properly sized for gallery use.
