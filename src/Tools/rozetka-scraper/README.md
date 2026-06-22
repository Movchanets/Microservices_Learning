# Rozetka Scraper

Standalone Playwright scraper for Rozetka.com.ua that extracts real product data for the Marketplace Seeder.App.

## Features

- **Category Tree**: Scrapes full category hierarchy
- **Product Listing**: Extracts product tiles from category pages with ad filtering
- **Product Details**: SKU, full image gallery, breadcrumbs, specifications, variants
- **Typed Attributes**: Dynamic attribute extraction with type inference (color, number, text, boolean, list, resolution)
- **Anti-Bot Evasion**: Realistic browser fingerprint, random delays
- **Idempotent**: Skips existing products and images

## Structure

```
rozetka-scraper/
├── pages/
│   ├── rozetka-categories.page.ts    # Category tree POM
│   ├── rozetka-category.page.ts      # Category listing POM
│   └── rozetka-product.page.ts       # Product detail POM
├── fixtures/
│   └── scraper.fixture.ts            # Browser context with anti-bot
├── utils/
│   ├── image-downloader.ts           # Image download utility
│   └── rozetka-transformer.ts        # Data transformation + attribute system
├── scripts/
│   ├── rozetka-scraper.ts            # Main product scraper
│   └── scrape-categories.ts          # Category tree scraper
├── index.ts                          # Public API exports
├── package.json
└── tsconfig.json
```

## Quick Start

```bash
cd src/Tools/rozetka-scraper
npm install
npx playwright install chromium

# Scrape products (default: 10 laptops)
npm run scrape
npm run scrape -- --category phones --limit 5

# Scrape category tree
npm run scrape:categories
npm run scrape:categories -- --depth 2
```

## CLI Options

### rozetka-scraper.ts

| Option | Default | Description |
|--------|---------|-------------|
| `-c, --category` | laptops | Category: laptops, phones, tablets, headphones |
| `-l, --limit` | 10 | Max products to scrape |

### scrape-categories.ts

| Option | Default | Description |
|--------|---------|-------------|
| `-d, --depth` | 1 | Tree depth to scrape (1 = top-level only) |

## Output

All output is written to `src/Tools/Seeder.App/Data/`:
- `products.json` — product data with attributes, variants, images
- `rozetka-categories.json` — category tree
- `Images/` — downloaded product images
