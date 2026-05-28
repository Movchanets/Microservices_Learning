# Rozetka Product Scraper - Implementation Plan

## Overview

This document describes the implementation of a standalone Playwright scraper that extracts real products from Rozetka.com.ua and prepares data for the Marketplace Seeder.App.

## Goals

1. **Real Product Data**: Replace placeholder products with real Rozetka products
2. **Local Images**: Download product images locally for Media API upload
3. **Idempotency**: Support resuming interrupted scrapes safely
4. **Compatibility**: Generate JSON matching Seeder.App's ProductModel format

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Rozetka.com.ua                            │
│  (Category Pages → Product Detail Pages → Image URLs)       │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              rozetka-scraper.ts (Playwright)                 │
│  • Navigate category pages                                  │
│  • Extract product listings                                 │
│  • Visit detail pages for images/description                │
│  • Download images to local filesystem                      │
│  • Generate products.json                                   │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              src/Tools/Seeder.App/Data/                      │
│  ├── products.json          (generated)                     │
│  └── Images/                                                │
│      ├── {sku-slug-1}/                                      │
│      │   ├── image1.jpg                                     │
│      │   ├── image2.jpg                                     │
│      │   └── image3.jpg                                     │
│      └── {sku-slug-2}/                                      │
│          └── image1.jpg                                     │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              Seeder.App (C# Worker)                          │
│  • Reads products.json                                      │
│  • Uploads images to Media API                              │
│  • Creates products via Catalog API                         │
└─────────────────────────────────────────────────────────────┘
```

## File Locations

| File | Location | Purpose |
|------|----------|---------|
| Scraper Script | `tests/E2ETests/scripts/rozetka-scraper.ts` | Main TypeScript scraper |
| Package Config | `tests/E2ETests/scripts/package.json` | Node.js dependencies |
| TypeScript Config | `tests/E2ETests/scripts/tsconfig.json` | TypeScript configuration |
| Output JSON | `src/Tools/Seeder.App/Data/products.json` | Generated product data |
| Output Images | `src/Tools/Seeder.App/Data/Images/` | Downloaded product images |

## Data Flow

### 1. Scraping Phase

```
Category URL
    ↓
[Page 1] → Extract product tiles → Get title, price, URL, thumbnail
    ↓
[Page 2] → Extract product tiles → ...
    ↓
...
    ↓
[Detail Page] → Extract description, full image gallery, article number
```

### 2. Download Phase

```
Product Image URLs
    ↓
Download to: src/Tools/Seeder.App/Data/Images/{sku-slug}/image{N}.jpg
    ↓
Update ImageUrl in JSON to relative path: Images/{sku-slug}/image1.jpg
```

### 3. Output Phase

```json
{
  "StoreName": "Tech Store",
  "CategoryName": "Electronics",
  "Name": "Ноутбук ASUS ROG Strix G16",
  "Description": "Ігровий ноутбук з RTX 4070...",
  "Price": 45999,
  "Currency": "UAH",
  "Sku": "ROZ-ROGSTRIXG16",
  "Tags": ["laptop", "notebook", "computer", "asus"],
  "ImageUrl": "Images/laptop-asus-rog-strix-g16/image1.jpg",
  "InitialStock": 42
}
```

## Anti-Bot Strategy

### Browser Fingerprint

| Setting | Value | Reason |
|---------|-------|--------|
| User-Agent | Chrome 124 on Windows 10 | Most common browser |
| Viewport | 1920×1080 | Standard desktop resolution |
| Locale | uk-UA | Ukrainian locale for Rozetka |
| Timezone | Europe/Kiev | Ukrainian timezone |

### Behavioral Patterns

| Pattern | Implementation | Reason |
|---------|----------------|--------|
| Random Delays | 1-3 seconds between actions | Mimics human reading time |
| Page Load Wait | `waitUntil: 'networkidle'` | Ensures dynamic content loads |
| Scroll Behavior | Scroll to elements before click | Natural interaction pattern |
| WebDriver Hidden | `navigator.webdriver = false` | Bypass basic bot detection |

## Idempotency Mechanism

### Product Level

```typescript
// Before scraping a product:
const sku = generateSku(title, article);
if (existingSkus.has(sku)) {
  log('Product already exists, skipping');
  return null;
}
```

### Image Level

```typescript
// Before downloading an image:
const imagePath = path.join(IMAGES_DIR, skuSlug, `image${index}.jpg`);
if (await fs.pathExists(imagePath)) {
  log('Image already exists, skipping');
  return relativePath;
}
```

### Resume Safety

- Script can be interrupted at any point
- Re-running will skip already-scraped products
- Partial image downloads are detected and re-downloaded
- products.json is updated incrementally

## SKU Generation Strategy

### With Article Number (Preferred)

```
ROZ-{article_number}
Example: ROZ-391478521 → ROZ-391478521
```

### Without Article Number (Fallback)

```
ROZ-{first_20_chars_of_slug}
Example: "ASUS ROG Strix G16" → ROZ-ASUSROGSTRIXG16
```

## Supported Categories

| Key | Name | URL | Store | Category |
|-----|------|-----|-------|----------|
| laptops | Laptops | /ua/notebooks/c80004/ | Tech Store | Electronics |
| phones | Smartphones | /ua/mobile-phones/c80259/ | Tech Store | Electronics |
| tablets | Tablets | /ua/tablets/c80033/ | Tech Store | Electronics |
| headphones | Headphones | /ua/headphones/c80027/ | Tech Store | Electronics |

## Error Handling

### Network Errors

```typescript
try {
  await page.goto(url, { timeout: 60000 });
} catch (error) {
  log(`Navigation failed: ${error}`, 'warn');
  return product; // Return partial data
}
```

### Missing Selectors

```typescript
// Multiple fallback selectors
const title = tile.querySelector(
  '.goods-tile__title, [data-testid="product-title"], a[title]'
);
```

### Download Failures

```typescript
try {
  await downloadImage(url, slug, index);
} catch (error) {
  log(`Download failed: ${error}`, 'warn');
  // Continue with other images
}
```

## Testing Strategy

### Manual Testing

```bash
# Test with small batch
npx tsx rozetka-scraper.ts --category laptops --limit 2

# Test with visible browser (debugging)
npx tsx rozetka-scraper.ts --category phones --limit 1 --no-headless
```

### Verification Checklist

- [ ] Script runs without errors
- [ ] products.json is created/updated
- [ ] Images are downloaded to correct directory
- [ ] SKU codes are unique
- [ ] Prices are parsed correctly (in UAH)
- [ ] Idempotency works (re-run skips existing)
- [ ] JSON format matches ProductModel

## Integration with Seeder.App

### Current Seeder.App Usage

The Seeder.App's `Worker.cs` reads `products.json` and:
1. Creates products via Catalog API
2. Creates SKUs for each product
3. Sets up inventory

### Required Changes

**None** - The scraper outputs JSON in the exact format expected by `ProductModel`:

```csharp
public record ProductModel(
    string StoreName,
    string CategoryName,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string Sku,
    string[] Tags,
    string ImageUrl,  // Now local path instead of URL
    int InitialStock
);
```

### Media API Integration

The `ImageUrl` field now contains a local relative path (e.g., `Images/laptop-asus-rog/image1.jpg`). The Seeder.App needs to be extended to:

1. Detect local vs remote URLs
2. Upload local images to Media API
3. Replace ImageUrl with Media API URL

**Note**: This Media API integration is a separate task and not part of this scraper.

## Dependencies

### Node.js Packages

| Package | Version | Purpose |
|---------|---------|---------|
| playwright | ^1.43.0 | Browser automation |
| fs-extra | ^11.2.0 | File system operations |
| commander | ^12.0.0 | CLI argument parsing |
| tsx | ^4.7.0 | TypeScript execution |
| typescript | ^5.4.0 | TypeScript compiler |

### System Requirements

- Node.js 18+
- npm or pnpm
- Chromium browser (installed via Playwright)

## CLI Usage

### Basic Usage

```bash
cd tests/E2ETests/scripts
npm install
npx tsx rozetka-scraper.ts --category laptops --limit 20
```

### Options

| Option | Default | Description |
|--------|---------|-------------|
| `-c, --category` | laptops | Category to scrape |
| `-l, --limit` | 20 | Max products to scrape |
| `--no-headless` | false | Show browser window |

### Examples

```bash
# Scrape 10 laptops
npx tsx rozetka-scraper.ts -c laptops -l 10

# Scrape 5 phones with visible browser
npx tsx rozetka-scraper.ts -c phones -l 5 --no-headless

# Scrape tablets
npx tsx rozetka-scraper.ts --category tablets --limit 15
```

## Future Enhancements

1. **Parallel Scraping**: Multiple browser contexts for faster scraping
2. **Proxy Support**: Rotate proxies for large-scale scraping
3. **Category Discovery**: Auto-discover available categories
4. **Price Tracking**: Monitor price changes over time
5. **Media API Upload**: Direct upload to Media API during scrape
6. **Specs Extraction**: Extract detailed specifications
7. **Review Scraping**: Collect product reviews

## Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Timeout errors | Slow network | Increase timeout in script |
| Empty descriptions | Selector changed | Update selectors in evaluate() |
| Missing images | CDN blocking | Check image URL format |
| Duplicate SKUs | Article collision | Add random suffix |

### Debug Mode

```bash
# Run with visible browser and verbose logging
npx tsx rozetka-scraper.ts --no-headless -c laptops -l 1
```

## References

- [Rozetka.com.ua](https://rozetka.com.ua/)
- [Playwright Documentation](https://playwright.dev/)
- [Seeder.App Source](src/Tools/Seeder.App/)
- [ProductModel Definition](src/Tools/Seeder.App/Models/Models.cs)
