# Rozetka Scraper — Plan and Alignment Summary

## What We Built

A Playwright-based scraper that extracts real products from Rozetka.com.ua and feeds them into the Marketplace Seeder.App. The pipeline:

```
Rozetka.com.ua -> Playwright Scraper -> products.json + Images/ -> Seeder.App -> Catalog API
```

### Components

| Component | Location | Purpose |
|-----------|----------|---------|
| Category POM | `src/Tools/rozetka-scraper/pages/rozetka-category.page.ts` | Listing page extraction |
| Product POM | `src/Tools/rozetka-scraper/pages/rozetka-product.page.ts` | Detail page (SKU, gallery, breadcrumbs, variants) |
| Transformer | `src/Tools/rozetka-scraper/utils/rozetka-transformer.ts` | Rozetka to SeederProduct mapping |
| Image Downloader | `src/Tools/rozetka-scraper/utils/image-downloader.ts` | Image download to local filesystem |
| Main Scraper | `src/Tools/rozetka-scraper/scripts/rozetka-scraper.ts` | Two-phase orchestrator |
| C# Models | `src/Tools/Seeder.App/Models/Models.cs` | ProductModel, VariantModel |
| Product Seeder | `src/Tools/Seeder.App/Seeders/ProductSeeder.cs` | Creates products + variant SKUs via API |
| Worker | `src/Tools/Seeder.App/Worker.cs` | Orchestrates full seed pipeline |

---

## Data Flow and Model Alignment

### TypeScript -> JSON -> C# Field Mapping

| TS `SeederProduct` | JSON field | C# `ProductModel` | Status |
|---------------------|------------|---------------------|--------|
| `StoreName` | `StoreName` | `StoreName` | Aligned |
| `CategoryName` | `CategoryName` | `CategoryName` | Aligned |
| `Name` | `Name` | `Name` | Aligned |
| `Description` | `Description` | `Description` | Aligned |
| `Price` | `Price` | `Price` | Aligned |
| `Currency` | `Currency` | `Currency` | Aligned |
| `Sku` | `Sku` | `Sku` | Aligned |
| `Tags` | `Tags` | `Tags` | Aligned |
| `ImageUrl` | `ImageUrl` | `ImageUrl` | Aligned |
| `InitialStock` | `InitialStock` | `InitialStock` | Aligned |
| `Variants` | `Variants` | `Variants` | Aligned |
| `RozetkaCode` | `RozetkaCode` | **Missing in C#** | Gap |
| `Gallery` | `Gallery` | **Missing in C#** | Gap |
| `Breadcrumbs` | `Breadcrumbs` | **Missing in C#** | Gap |
| `CategoryPath` | `CategoryPath` | **Missing in C#** | Gap |

### Variant Field Mapping

| TS Variant | JSON Variant | C# `VariantModel` | Status |
|------------|--------------|---------------------|--------|
| `RozetkaCode` | `RozetkaCode` | `RozetkaCode` | Aligned |
| `Name` | `Name` | `Name` | Aligned |
| `Type` | `Type` | `Type` | Aligned |
| `Price` | `Price` | `Price` | Aligned |
| `ImageUrl` | `ImageUrl` | `ImageUrl` | Aligned |
| `Gallery` | `Gallery` | `Gallery` | Aligned |

---

## Gaps and Issues Found

### 1. Scraper transformer creates variants with empty images

**Location**: `utils/rozetka-transformer.ts` lines 107-113

The `toSeederProduct` function maps variants with `ImageUrl: ''` and `Gallery: []`. The main scraper's `scrapeProduct()` method never visits variant URLs to fill these. Only the test script (`test-variants.ts`) does variant image scraping.

**Fix**: Update `scrapeProduct()` in the main scraper to iterate `details.variants`, visit each variant URL with a fresh context, extract gallery, download images, and update the variant fields.

### 2. Variant price always uses parent price

**Location**: `utils/rozetka-transformer.ts` line 111

`Price: parsePrice(tile.priceText)` uses the listing page price, not the variant-specific price. Different storage sizes (256GB vs 1TB) have different prices.

**Fix**: Visit each variant page and extract its actual price.

### 3. C# ProductModel missing fields for full Rozetka data

The C# model lacks:
- `RozetkaCode` — original Rozetka product ID (useful for dedup/debugging)
- `Gallery` — array of all image paths (only `ImageUrl` exists)
- `Breadcrumbs` — category hierarchy from JSON-LD
- `CategoryPath` — human-readable path string

**Impact**: The seeder only uploads one image per product. Gallery images are lost.

**Options**:
- A. Add fields to `ProductModel` (requires JSON schema update)
- B. Keep extra fields in JSON but ignore in C# (current state — works but wasteful)
- C. Extend Catalog API to accept gallery arrays

### 4. Category matching is fragile

**Location**: `Worker.cs` — `FindBestCategory()`

The function tries exact match, last breadcrumb segment, partial match, first segment. Problems:
- "Ноутбуки Acer" doesn't match "Electronics" (from categories.json)
- Creates many narrow categories instead of mapping to existing broad ones

**Fix**: Add a mapping layer that maps Rozetka breadcrumb names to existing category names. Create `Data/category-mapping.json`.

### 5. Variant filtering needs improvement

The `extractVariants` POM method filters accessories by Ukrainian keywords, but some get through. Service products, cases, and cables sometimes pass the filter.

**Fix**: Add more robust filtering by class name (`tile-image-host`), URL pattern, and product family slug matching.

### 6. InitialStock is random

`Math.floor(Math.random() * 90) + 10` should be a fixed value or derived from Rozetka availability.

### 7. Variant image directories use PID instead of name

Directories like `Images/543560755/` should be `Images/iphone-17-pro-max-deep-blue/` for readability.

---

## Improvement Plan

### Phase 1: Fix variant image pipeline in main scraper
- [ ] Update `scrapeProduct()` to visit variant URLs and download images
- [ ] Extract variant-specific price from each variant page
- [ ] Use variant name (not PID) for image directory slugs
- **Files**: `scripts/rozetka-scraper.ts`

### Phase 2: Extend C# models for full data
- [ ] Add `Gallery`, `Breadcrumbs`, `CategoryPath` to `ProductModel`
- [ ] Update `ProductSeeder` to upload multiple images via Media API
- [ ] Map variant images to SKU-specific media
- **Files**: `Models/Models.cs`, `Seeders/ProductSeeder.cs`, `Worker.cs`

### Phase 3: Improve category alignment
- [ ] Create Rozetka-to-Seeder category mapping config
- [ ] Update `FindBestCategory()` to use mapping before fallback
- [ ] Add parent category support (e.g., "Ноутбуки Acer" maps to "Electronics")
- **Files**: `Worker.cs`, new `Data/category-mapping.json`

### Phase 4: Robustness improvements
- [ ] Better variant filtering (exclude accessories, service products)
- [ ] Validate scraped data before writing to JSON
- [ ] Add retry logic for failed image downloads
- [ ] Log scrape statistics (success rate, variant count, image count)
- **Files**: `pages/rozetka-product.page.ts`, `utils/image-downloader.ts`

### Phase 5: Media API integration
- [ ] Extend `ProductSeeder` to call Media API for image upload
- [ ] Map gallery images to product media
- [ ] Map variant images to SKU-specific media
- **Files**: `Seeders/ProductSeeder.cs`, new `Seeders/MediaSeeder.cs`

---

## Architecture Decisions

| Decision | Rationale |
|----------|-----------|
| Two-phase scraping (collect URLs then visit products) | Rozetka CAPTCHA blocks category-to-product navigation |
| Fresh browser context per product | Avoids anti-bot detection |
| Local image storage before upload | Decouples scraping from API availability |
| Variant SKUs on same product | Matches Catalog API product-to-SKU relationship |
| Breadcrumb-based categories | More accurate than hardcoded category mapping |

---

## Test Results (Last Run)

| Product | Category | Gallery | Breadcrumbs | Variants | Variant Images |
|---------|----------|---------|-------------|----------|----------------|
| Acer Nitro Lite | Ноутбуки > Acer | 10 | 5 | 0 | — |
| ASUS TUF Gaming | Ноутбуки > ASUS | 10 | 5 | 0 | — |
| Lenovo IdeaPad | Ноутбуки > Lenovo | 8 | 5 | 0 | — |
| Apple AirPods 4 | Навушники > Apple | 8 | 6 | 0 | — |
| Samsung Buds3 Pro | Навушники > Samsung | 10 | 6 | 0 | — |
| Logitech G735 | Навушники > Logitech | 9 | 6 | 0 | — |
| iPhone 17 Pro Max | Телефони > Apple | 10 | 6 | 6 | 10 each |
