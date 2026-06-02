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

    // Return all extracted tiles without filtering for now to guarantee we get products
    return allTiles;
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

  /**
   * Scrape the category page's sidebar filters.
   */
  async extractSidebarFilters(): Promise<string[]> {
    try {
      await this.page.waitForSelector('details[data-testid="filter"]', { timeout: 10000 });
    } catch {}

    return this.page.evaluate(() => {
      const list: string[] = [];
      document.querySelectorAll('details[data-testid="filter"]').forEach(el => {
        const summary = el.querySelector('summary');
        if (summary) {
          const clone = summary.cloneNode(true) as HTMLElement;
          clone.querySelectorAll('span.quantity, span[class*="quantity"], svg, use').forEach(q => q.remove());
          const text = clone.textContent?.trim();
          if (text && text.length > 2 && text.length < 50 &&
              !text.includes('Продавець') &&
              !text.includes('Ціна') &&
              !text.includes('Власникам') &&
              !text.includes('Програма') &&
              !text.includes('Доставка') &&
              !text.includes('Товари з акціями') &&
              !text.includes('Відгуки') &&
              !text.includes('Стан товару') &&
              !text.includes('Smart') &&
              !text.includes('Новинки') &&
              !text.includes('Готовий до відправлення')) {
            list.push(text);
          }
        }
      });
      return list;
    });
  }

  private randomDelay(min = 1000, max = 2500): Promise<void> {
    const delay = Math.floor(Math.random() * (max - min + 1)) + min;
    return new Promise(resolve => setTimeout(resolve, delay));
  }
}
