/**
 * Rozetka Category Page Object
 *
 * Handles product listing extraction and pagination from Rozetka category pages.
 * Includes ad/sponsored content filtering and brand extraction.
 */

import { Page } from 'playwright';

export interface ProductTile {
  title: string;
  priceText: string;
  url: string;
  imgSrc: string;
  articleId: string;  // Rozetka product ID (e.g., p528975609)
  brand: string;
}

/**
 * Known brand patterns for product validation.
 * Used to filter out sponsored ads that don't match the expected product type.
 */
const BRAND_PATTERNS: Record<string, RegExp[]> = {
  laptops: [
    /^(Ноутбук|Notebook|Laptop)/i,
    /\b(Acer|ASUS|Lenovo|Apple|HP|Dell|MSI|MacBook|ThinkPad|IdeaPad|TUF|ROG|Swift|Aspire|Nitro)\b/i,
  ],
  phones: [
    /^(Мобільний телефон|Смартфон|Телефон)/i,
    /\b(iPhone|Samsung Galaxy|Xiaomi|Redmi|POCO|Pixel|OnePlus|Huawei|Honor|Realme|Motorola|Nokia)\b/i,
  ],
  tablets: [
    /^(Планшет|Tablet)/i,
    /\b(iPad|Galaxy Tab|Redmi Pad|Lenovo Tab|Idea Tab|MediaPad|MatePad)\b/i,
  ],
  headphones: [
    /^(Навушники|Headphones|Earbuds)/i,
    /\b(AirPods|Galaxy Buds|Sony|Bose|Sennheiser|JBL|Beats|Hator|Logitech|HyperX|SteelSeries)\b/i,
  ],
};

/**
 * Words that indicate sponsored/ad content (not real products in the category).
 */
const AD_INDICATORS = [
  'сумка', 'чохол', 'кабель', 'заряд', 'підставка', 'тримач',
  'фотоплівка', 'картридж', 'фотопапір', 'рукав', 'серветка',
  'bag', 'case', 'cable', 'charger', 'stand', 'holder', 'film',
];

/**
 * Well-known brands for auto-detection from product titles.
 */
const KNOWN_BRANDS = [
  'Apple', 'Samsung', 'Xiaomi', 'Lenovo', 'ASUS', 'Acer', 'HP', 'Dell', 'MSI',
  'Sony', 'Logitech', 'JBL', 'Bose', 'Sennheiser', 'Hator', 'HyperX', 'SteelSeries',
  'iPhone', 'iPad', 'MacBook', 'Galaxy', 'Redmi', 'POCO', 'Pixel', 'OnePlus',
  'Huawei', 'Honor', 'Realme', 'Motorola', 'Nokia', 'AirPods', 'ThinkPad',
  'IdeaPad', 'TUF', 'ROG', 'Swift', 'Aspire', 'Nitro',
];

export class RozetkaCategoryPage {
  readonly page: Page;
  private categoryKey: string = '';

  constructor(page: Page) {
    this.page = page;
  }

  async goto(url: string, categoryKey?: string): Promise<void> {
    this.categoryKey = categoryKey || '';
    await this.page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await this.page.waitForSelector('main article', { timeout: 30000 });
    await this.randomDelay(1500, 2500);
  }

  /**
   * Check if a tile looks like a real product (not a sponsored ad).
   */
  private isValidProduct(tile: ProductTile): boolean {
    const titleLower = tile.title.toLowerCase();

    // Reject if title contains ad indicator words
    if (AD_INDICATORS.some(w => titleLower.includes(w))) {
      return false;
    }

    // If we have brand patterns for this category, validate against them
    const patterns = BRAND_PATTERNS[this.categoryKey];
    if (patterns && patterns.length > 0) {
      return patterns.some(p => p.test(tile.title));
    }

    // No patterns defined — accept all non-ad tiles
    return true;
  }

  /**
   * Extract brand from a product title.
   * Tries known brand names first, then falls back to first word.
   */
  private extractBrandFromTitle(title: string): string {
    // Try known brands (longest match first)
    for (const brand of KNOWN_BRANDS) {
      if (title.includes(brand)) return brand;
    }

    // Try first word if it looks like a brand (capitalized, 2+ chars)
    const firstWord = title.split(/\s/)[0];
    if (firstWord && firstWord.length >= 2 && /^[A-ZА-ЯІЇЄҐ]/.test(firstWord)) {
      return firstWord;
    }

    return '';
  }

  /**
   * Extract all product tiles from current listing page.
   * Filters out sponsored ads and non-matching products.
   */
  async extractProductTiles(): Promise<ProductTile[]> {
    const allTiles = await this.page.evaluate(() => {
      const articles = document.querySelectorAll('main article');
      const results: Array<{
        title: string;
        priceText: string;
        url: string;
        imgSrc: string;
        articleId: string;
        brand: string;
      }> = [];

      articles.forEach(article => {
        // Find product link — pattern: /ua/{slug}/{product-id}/
        const allLinks = Array.from(article.querySelectorAll('a[href]'));
        const prodLink = allLinks.find(a => /\/p\d+\//.test(a.getAttribute('href') || ''));
        if (!prodLink) return;

        const href = prodLink.getAttribute('href') || '';
        const idMatch = href.match(/\/p(\d+)\//);
        if (!idMatch) return;

        // Title from image alt (most reliable)
        const img = article.querySelector('img[alt]');
        const imgAlt = img?.getAttribute('alt') || '';
        const imgSrc = img?.getAttribute('src') || '';

        // Price text — try multiple selectors
        let priceText = '';
        const priceEl = article.querySelector(
          '[class*="price"] span, [class*="price"], [class*="cost"]'
        );
        if (priceEl) {
          const text = priceEl.textContent || '';
          const match = text.match(/(\d[\d\s]*₴[\d\s₴]*)/);
          if (match) priceText = match[1];
        }
        // Fallback: search article text
        if (!priceText) {
          const text = article.textContent || '';
          const match = text.match(/(\d[\d\s]*₴[\d\s₴]*)/);
          if (match) priceText = match[1];
        }

        // Brand extraction from tile:
        // Method 1: Brand logo image alt
        let brand = '';
        const brandImg = article.querySelector(
          '[class*="brand"] img, [class*="logo"] img, [class*="producer"] img'
        );
        if (brandImg) {
          brand = brandImg.getAttribute('alt') || brandImg.getAttribute('title') || '';
        }
        // Method 2: Brand text element
        if (!brand) {
          const brandEl = article.querySelector(
            '[class*="brand"], [class*="producer"], [class*="manufacturer"]'
          );
          const brandText = brandEl?.textContent?.trim() || '';
          if (brandText && brandText.length > 1 && brandText.length < 50) {
            brand = brandText;
          }
        }
        // Method 3: Extract from title (fallback)
        if (!brand) {
          const knownBrands = [
            'Apple', 'Samsung', 'Xiaomi', 'Lenovo', 'ASUS', 'Acer', 'HP', 'Dell', 'MSI',
            'Sony', 'Logitech', 'JBL', 'Bose', 'Sennheiser', 'Hator', 'HyperX',
            'iPhone', 'iPad', 'MacBook', 'Galaxy', 'Redmi', 'POCO', 'Pixel',
          ];
          for (const b of knownBrands) {
            if (imgAlt.includes(b)) { brand = b; break; }
          }
        }

        results.push({
          title: imgAlt.substring(0, 300),
          priceText,
          url: href.startsWith('http') ? href : `https://rozetka.com.ua${href}`,
          imgSrc,
          articleId: `p${idMatch[1]}`,
          brand,
        });
      });

      return results;
    });

    // Filter out ads and non-matching products
    return allTiles.filter(t => this.isValidProduct(t));
  }

  /**
   * Navigate to next page.
   * @returns true if navigated, false if no next page
   */
  async nextPage(): Promise<boolean> {
    const next = await this.page.$(
      'a[aria-label="Next"], a.pagination__next, [class*="pagination"] a:last-child'
    );
    if (!next) return false;

    await next.scrollIntoViewIfNeeded();
    await this.randomDelay();
    await next.click();
    await this.page.waitForLoadState('domcontentloaded');
    await this.randomDelay(2000, 3000);
    return true;
  }

  private randomDelay(min = 1000, max = 2500): Promise<void> {
    const delay = Math.floor(Math.random() * (max - min + 1)) + min;
    return new Promise(resolve => setTimeout(resolve, delay));
  }
}
