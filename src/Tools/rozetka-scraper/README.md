# Rozetka Scraper

Standalone Playwright scraper for Rozetka.com.ua that extracts real product data for the Marketplace Seeder.App.

## Features

- **Full Image Gallery**: Downloads all product images in `/big/` resolution (not just preview)
- **Product SKU**: Extracts actual Rozetka article code (Код товару)
- **Breadcrumbs**: Captures category hierarchy from JSON-LD structured data
- **Category Tree**: Scrapes full category hierarchy
- **Anti-Bot Evasion**: Realistic browser fingerprint, random delays
- **Idempotent**: Skips existing products and images

## Structure

```
rozetka-scraper/
├── pages/
│   ├── rozetka-category.page.ts    # Category listing POM
│   ├── rozetka-product.page.ts     # Product detail POM
│   └── rozetka-categories.page.ts  # Category tree POM
├── fixtures/
│   └── scraper.fixture.ts          # Browser context with anti-bot
├── utils/
│   ├── image-downloader.ts         # Image download utility
│   └── rozetka-transformer.ts      # Data transformation
├── scripts/
│   ├── rozetka-scraper.ts          # Main product scraper
│   └── scrape-categories.ts        # Category tree scraper
└── data/                           # Output directory
```

## Quick Start

```bash
cd src/Tools/rozetka-scraper
npm install
npx playwright install chromium

# Scrape 10 laptops
npx tsx scripts/rozetka-scraper.ts --category laptops --limit 10

# Scrape category tree
npx tsx scripts/scrape-categories.ts
```

## Output Format

### products.json

```json
{
  "StoreName": "Tech Store",
  "CategoryName": "Комп'ютери та ноутбуки > Ноутбуки",
  "Name": "Ноутбук Acer Nitro Lite NL16-71G-56P7",
  "Description": "...",
  "Price": 37999,
  "Currency": "UAH",
  "Sku": "ROZ-528975609",
  "RozetkaCode": "528975609",
  "Tags": ["laptop", "notebook", "acer", "ноутбуки"],
  "ImageUrl": "Images/acer-nitro-lite/image0.jpg",
  "Gallery": [
    "Images/acer-nitro-lite/image0.jpg",
    "Images/acer-nitro-lite/image1.jpg",
    "Images/acer-nitro-lite/image2.jpg"
  ],
  "Breadcrumbs": [
    { "name": "Комп'ютери та ноутбуки", "url": "...", "position": 2 },
    { "name": "Ноутбуки", "url": "...", "position": 3 }
  ],
  "CategoryPath": "Комп'ютери та ноутбуки > Ноутбуки > Ноутбуки Acer",
  "InitialStock": 42
}
```

### rozetka-categories.json

```json
[
  {
    "name": "Комп'ютери та ноутбуки",
    "url": "https://rozetka.com.ua/ua/computers-notebooks/c80253/",
    "id": "c80253",
    "level": 0,
    "children": [
      {
        "name": "Ноутбуки",
        "url": "https://rozetka.com.ua/ua/notebooks/c80004/",
        "id": "c80004",
        "level": 1,
        "children": []
      }
    ]
  }
]
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
