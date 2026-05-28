# Quick Start Guide - Rozetka Scraper

## Prerequisites

- Node.js 18+ installed
- npm or pnpm package manager
- Internet connection to Rozetka.com.ua

## Step 1: Install Dependencies

```bash
cd tests/E2ETests/scripts
npm install
```

This installs:
- Playwright (browser automation)
- fs-extra (file operations)
- commander (CLI parsing)
- tsx (TypeScript execution)

## Step 2: Install Playwright Browsers

```bash
npx playwright install chromium
```

## Step 3: Run the Scraper

### Basic Usage

```bash
# Scrape 20 laptops (default)
npx tsx rozetka-scraper.ts

# Scrape specific category
npx tsx rozetka-scraper.ts --category phones

# Scrape with custom limit
npx tsx rozetka-scraper.ts --category laptops --limit 10
```

### Available Categories

| Key | Description |
|-----|-------------|
| `laptops` | Ноутбуки (Laptops) |
| `phones` | Смартфони (Smartphones) |
| `tablets` | Планшети (Tablets) |
| `headphones` | Навушники (Headphones) |

## Step 4: Verify Output

After scraping completes, check:

```bash
# Products JSON
cat src/Tools/Seeder.App/Data/products.json

# Downloaded images
ls -la src/Tools/Seeder.App/Data/Images/
```

## Step 5: Run Seeder.App

```bash
# Build and run the seeder
cd src/Tools/Seeder.App
dotnet run
```

The Seeder.App will:
1. Read products.json
2. Create products via Catalog API
3. Upload images to Media API (if implemented)

## Example Session

```bash
$ cd tests/E2ETests/scripts
$ npm install
$ npx playwright install chromium
$ npx tsx rozetka-scraper.ts --category laptops --limit 5

ℹ️ [2026-05-27T10:00:00.000Z] Initializing Rozetka Scraper...
ℹ️ [2026-05-27T10:00:01.000Z] Browser initialized successfully
ℹ️ [2026-05-27T10:00:01.000Z] Starting scrape for category: Laptops (limit: 5)
ℹ️ [2026-05-27T10:00:02.000Z] Navigating to category: https://rozetka.com.ua/ua/notebooks/c80004/
ℹ️ [2026-05-27T10:00:05.000Z] Scraping page 1...
ℹ️ [2026-05-27T10:00:06.000Z] Found 5 products on page 1 (total: 5)
ℹ️ [2026-05-27T10:00:06.000Z] Processing product 1/5
ℹ️ [2026-05-27T10:00:08.000Z] Scraping details for: Ноутбук ASUS ROG Strix G16...
ℹ️ [2026-05-27T10:00:10.000Z] Downloaded: laptop-asus-rog-strix-g16/image1.jpg
ℹ️ [2026-05-27T10:00:11.000Z] Downloaded: laptop-asus-rog-strix-g16/image2.jpg
...
ℹ️ [2026-05-27T10:05:00.000Z] Saved 5 new products to products.json (total: 18)
ℹ️ [2026-05-27T10:05:00.000Z] Scraping completed successfully!
ℹ️ [2026-05-27T10:05:01.000Z] Browser closed
```

## Troubleshooting

### Problem: "Browser not found"

```bash
# Install Chromium
npx playwright install chromium
```

### Problem: Timeout errors

The scraper waits for pages to load. If your network is slow:

1. Check internet connection
2. Try with `--limit 1` first
3. Run with `--no-headless` to see what's happening

### Problem: Empty products.json

Possible causes:
- Rozetka selectors changed (site updated)
- Network blocking
- Rate limiting

Solution: Run with `--no-headless` to debug

### Problem: Images not downloading

Check:
- Image URLs in browser console
- Network permissions
- Disk space

## Advanced Usage

### Debug Mode (Visible Browser)

```bash
npx tsx rozetka-scraper.ts --no-headless --limit 1
```

This opens a visible browser window so you can see what's happening.

### Resuming Interrupted Scrapes

Simply run the same command again:

```bash
npx tsx rozetka-scraper.ts --category laptops --limit 20
```

The scraper will:
- Skip products already in products.json
- Skip images already downloaded
- Continue where it left off

### Scraping Multiple Categories

```bash
# Scrape different categories
npx tsx rozetka-scraper.ts --category laptops --limit 10
npx tsx rozetka-scraper.ts --category phones --limit 10
npx tsx rozetka-scraper.ts --category tablets --limit 10
```

Each run appends to products.json.

## Output Format

### products.json

```json
[
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
]
```

### Image Directory Structure

```
src/Tools/Seeder.App/Data/Images/
├── laptop-asus-rog-strix-g16/
│   ├── image1.jpg
│   ├── image2.jpg
│   └── image3.jpg
├── smartphone-iphone-15-pro/
│   ├── image1.jpg
│   └── image2.jpg
└── tablet-ipad-pro/
    └── image1.jpg
```

## Next Steps

After scraping:

1. **Verify data**: Check products.json has correct format
2. **Check images**: Ensure images downloaded correctly
3. **Run Seeder**: `cd src/Tools/Seeder.App && dotnet run`
4. **Verify in app**: Check products appear in marketplace

## Support

For issues:
- Check [Troubleshooting](#troubleshooting) section
- Run with `--no-headless` to debug
- Check Rozetka.com.ua is accessible
- Verify Node.js version (18+)
