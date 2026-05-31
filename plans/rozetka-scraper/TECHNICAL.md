# Technical Implementation Details

## Script Architecture

### Class Structure

```
RozetkaScraper
├── initialize()           # Setup browser and load existing data
├── scrapeProductListings() # Extract products from category pages
├── scrapeProductDetails()  # Get full details from product pages
├── downloadImage()         # Download single image to filesystem
├── processProduct()        # Full pipeline for one product
├── scrape()               # Main entry point
└── cleanup()              # Close browser
```

### Data Flow

```
1. initialize()
   ├── Ensure directories exist
   ├── Load existing products.json
   └── Launch Playwright browser

2. scrape(category, limit)
   ├── scrapeProductListings()
   │   ├── Navigate to category URL
   │   ├── Extract product tiles from DOM
   │   ├── Parse titles, prices, URLs
   │   └── Handle pagination
   │
   ├── For each listing:
   │   └── processProduct()
   │       ├── Check idempotency (SKU exists?)
   │       ├── scrapeProductDetails()
   │       │   ├── Navigate to product page
   │       │   ├── Extract description
   │       │   ├── Extract image gallery
   │       │   └── Extract article number
   │       ├── downloadImage() × N
   │       │   ├── Check if image exists
   │       │   ├── Download via fetch()
   │       │   └── Save to filesystem
   │       └── Create SeederProduct object
   │
   └── Save updated products.json

3. cleanup()
   └── Close browser
```

## DOM Selectors

### Category Page (Listing)

```typescript
// Product tiles
'rz-catalog-tile, .goods-tile, [data-testid="product-grid"] > *'

// Title
'.goods-tile__title, [data-testid="product-title"], a[title]'

// Price
'.goods-tile__price-value, [data-testid="product-price"], .price__value'

// Product link
'a[href*="/p/"]'

// Image
'img' (within tile)
```

### Product Page (Details)

```typescript
// Description
'.product-about__brief, [data-testid="product-description"], .product__description'

// Main image
'.product-photo__slider img, [data-testid="product-image"] img'

// Gallery thumbnails
'.product-photo__slider-item img, .gallery__thumb img'

// Article/SKU
'.product-about__info-value, [data-testid="product-article"]'
```

### Pagination

```typescript
// Next page button
'a.pagination__next, button[aria-label="Next page"]'
```

## Anti-Bot Techniques

### 1. User-Agent Spoofing

```typescript
const USER_AGENT = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36';
```

### 2. WebDriver Property Removal

```typescript
await page.addInitScript(() => {
  Object.defineProperty(navigator, 'webdriver', { get: () => false });
});
```

### 3. Realistic Viewport

```typescript
const VIEWPORT = { width: 1920, height: 1080 };
```

### 4. Locale and Timezone

```typescript
const context = await browser.newContext({
  locale: 'uk-UA',
  timezoneId: 'Europe/Kiev',
});
```

### 5. Random Delays

```typescript
function randomDelay(min = 1000, max = 3000): Promise<void> {
  const delay = Math.floor(Math.random() * (max - min + 1)) + min;
  return new Promise(resolve => setTimeout(resolve, delay));
}
```

## Image Download Strategy

### Method: Browser Fetch API

```typescript
const response = await page.evaluate(async (url: string) => {
  const resp = await fetch(url);
  const blob = await resp.blob();
  const buffer = await blob.arrayBuffer();
  return Array.from(new Uint8Array(buffer));
}, imageUrl);

await fs.writeFile(imagePath, Buffer.from(response));
```

**Why browser fetch?**
- Uses browser's cookie jar (if needed)
- Same-origin policy handled automatically
- Consistent with browser session

### File Naming Convention

```
Images/{sku-slug}/image{N}.jpg

Examples:
Images/laptop-asus-rog-strix-g16/image1.jpg
Images/laptop-asus-rog-strix-g16/image2.jpg
Images/smartphone-iphone-15-pro/image1.jpg
```

### Idempotency Check

```typescript
const imagePath = path.join(IMAGES_DIR, skuSlug, `image${imageIndex}.jpg`);
if (await fs.pathExists(imagePath)) {
  log(`Image already exists: ${skuSlug}/image${imageIndex}.jpg`);
  return `Images/${skuSlug}/image${imageIndex}.jpg`;
}
```

## SKU Generation

### Algorithm

```typescript
function generateSku(title: string, article?: string): string {
  // 1. Prefer article number (unique identifier from Rozetka)
  if (article) {
    return `ROZ-${article}`.toUpperCase();
  }
  
  // 2. Fallback: slugify title
  const slug = slugify(title)
    .replace(/-/g, '')
    .substring(0, 20);
  return `ROZ-${slug}`.toUpperCase();
}
```

### Examples

| Title | Article | SKU |
|-------|---------|-----|
| Ноутбук ASUS ROG Strix G16 | 391478521 | ROZ-391478521 |
| iPhone 15 Pro 256GB | (none) | ROZ-IPHONE15PRO256GB |
| Samsung Galaxy S24 Ultra | SM-S926B | ROZ-SM-S926B |

## Price Parsing

### Input Formats

- `"45 999 ₴"` → `45999`
- `"1 299"` → `1299`
- `"₴ 899"` → `899`

### Parser

```typescript
function parsePrice(priceStr: string): number {
  const cleaned = priceStr.replace(/[^\d]/g, '');
  return parseInt(cleaned, 10) || 0;
}
```

## Error Handling Strategy

### Navigation Errors

```typescript
try {
  await page.goto(url, { waitUntil: 'networkidle', timeout: 60000 });
} catch (error) {
  log(`Navigation failed: ${error}`, 'warn');
  return product; // Return partial data, continue with others
}
```

### Selector Errors

```typescript
// Multiple fallback selectors
const title = tile.querySelector(
  '.goods-tile__title, [data-testid="product-title"], a[title]'
);

// Graceful fallback
const title = titleEl?.textContent?.trim() || titleEl?.getAttribute('title') || '';
```

### Download Errors

```typescript
try {
  await downloadImage(url, slug, index);
} catch (error) {
  log(`Download failed: ${error}`, 'warn');
  return null; // Skip this image, continue with others
}
```

## Performance Considerations

### Sequential Processing

Products are processed sequentially to:
- Avoid rate limiting
- Reduce memory usage
- Maintain order in products.json

### Delay Configuration

```typescript
const MIN_DELAY = 1000;  // Minimum 1 second
const MAX_DELAY = 3000;  // Maximum 3 seconds
```

Adjust based on:
- Network speed
- Rozetka rate limits
- Time constraints

### Memory Management

- Browser pages are reused (not created per product)
- Images are streamed to disk (not held in memory)
- products.json is written once at the end

## TypeScript Configuration

### Target and Module

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "bundler"
  }
}
```

### Why ESNext Module?

- Supports top-level await
- Compatible with tsx runtime
- Modern Node.js features

## Dependencies Deep Dive

### playwright

```json
"playwright": "^1.43.0"
```

- Browser automation
- Handles JavaScript rendering
- Built-in anti-detection features

### fs-extra

```json
"fs-extra": "^11.2.0"
```

- `ensureDir()`: Create directories recursively
- `pathExists()`: Check file existence
- `readJson()` / `writeJson()`: JSON file operations
- `writeFile()`: Write binary data (images)

### commander

```json
"commander": "^12.0.0"
```

- CLI argument parsing
- Help text generation
- Option validation

### tsx

```json
"tsx": "^4.7.0"
```

- TypeScript execution without compilation step
- Fast startup time
- ESM support

## Testing the Scraper

### Unit Tests (Future)

```typescript
describe('slugify', () => {
  it('should convert to lowercase', () => {
    expect(slugify('ASUS ROG')).toBe('asus-rog');
  });
  
  it('should remove special characters', () => {
    expect(slugify('iPhone 15 Pro!')).toBe('iphone-15-pro');
  });
});
```

### Integration Tests (Future)

```typescript
describe('RozetkaScraper', () => {
  it('should scrape product listings', async () => {
    const scraper = new RozetkaScraper();
    await scraper.initialize();
    const products = await scraper.scrapeProductListings(url, 5);
    expect(products.length).toBeGreaterThan(0);
  });
});
```

## Monitoring and Logging

### Log Levels

```typescript
function log(message: string, level: 'info' | 'warn' | 'error' = 'info'): void {
  const timestamp = new Date().toISOString();
  const prefix = level === 'error' ? '❌' : level === 'warn' ? '⚠️' : 'ℹ️';
  console.log(`${prefix} [${timestamp}] ${message}`);
}
```

### Key Metrics Logged

- Number of products found per page
- Number of products processed
- Number of images downloaded
- Errors and warnings
- Total time elapsed

## Future Optimizations

### 1. Parallel Image Downloads

```typescript
await Promise.all(
  images.map((url, i) => downloadImage(url, slug, i))
);
```

### 2. Browser Pool

```typescript
const browsers = await Promise.all(
  Array(3).fill(null).map(() => chromium.launch())
);
```

### 3. Request Interception

```typescript
await page.route('**/*.{png,jpg,jpeg,gif,svg}', route => {
  // Skip unnecessary images during scraping
  route.abort();
});
```

### 4. Caching

```typescript
const cache = new Map<string, ScrapedProduct>();
if (cache.has(url)) return cache.get(url);
```

## Security Considerations

### No Credentials Stored

- No API keys in script
- No cookies persisted
- No authentication required

### Local File Access Only

- Images saved to local filesystem
- No remote uploads during scraping
- No network calls except to Rozetka

### Rate Limiting Respect

- Random delays between requests
- Sequential processing
- Configurable limits

## Maintenance Guide

### Updating Selectors

If Rozetka changes their HTML structure:

1. Run with `--no-headless` to inspect
2. Open DevTools on product page
3. Find new selector for target element
4. Update selector in `evaluate()` callback
5. Test with `--limit 1`

### Adding New Categories

1. Find category URL on Rozetka
2. Add to `CATEGORIES` object
3. Set appropriate tags
4. Test with small batch

### Debugging Failed Scrapes

1. Run with `--no-headless`
2. Check console for errors
3. Verify selectors match DOM
4. Check network tab for failed requests
5. Verify images are accessible
