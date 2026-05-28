/**
 * Rozetka Category Page Object
 * 
 * Encapsulates interactions with Rozetka category listing pages.
 * Handles product tile extraction, pagination, and anti-bot evasion.
 */

import { Page, Locator } from '@playwright/test';

export interface ProductTile {
  title: string;
  priceText: string;
  url: string;
  imgSrc: string;
  articleId: string;
  brand: string;
}

export class RozetkaCategoryPage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  /**
   * Navigate to a category URL
   */
  async goto(url: string): Promise<void> {
    await this.page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await this.waitForProducts();
  }

  /**
   * Wait for product tiles to appear
   */
  async waitForProducts(): Promise<void> {
    await this.page.waitForSelector('main article', { timeout: 30000 });
  }

  /**
   * Extract all product tiles from the current page
   */
  async extractProductTiles(): Promise<ProductTile[]> {
    return this.page.evaluate(() => {
      const articles = document.querySelectorAll('main article');
      const results: ProductTile[] = [];

      articles.forEach(article => {
        const allLinks = Array.from(article.querySelectorAll('a[href]'));
        const prodLink = allLinks.find(a => {
          const h = a.getAttribute('href') || '';
          return /\/p\d+\//.test(h);
        });
        if (!prodLink) return;

        const href = prodLink.getAttribute('href') || '';
        const idMatch = href.match(/\/p(\d+)\//);
        if (!idMatch) return;

        const img = article.querySelector('img[alt]');
        const imgAlt = img?.getAttribute('alt') || '';
        const imgSrc = img?.getAttribute('src') || '';

        const text = article.textContent || '';
        const priceMatch = text.match(/(\d[\d\s]*₴[\d\s₴]*)/);
        const priceText = priceMatch ? priceMatch[1] : '';

        const brandMatch = imgAlt.match(/^(Acer|ASUS|Lenovo|Apple|HP|Dell|MSI|Samsung|Xiaomi|Huawei)/i);

        results.push({
          title: imgAlt.substring(0, 300),
          priceText,
          url: href.startsWith('http') ? href : `https://rozetka.com.ua${href}`,
          imgSrc,
          articleId: `p${idMatch[1]}`,
          brand: brandMatch?.[1] || '',
        });
      });

      return results;
    });
  }

  /**
   * Navigate to the next page if available
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
   * Check if there are more products to load
   */
  async hasMoreProducts(currentCount: number): Promise<boolean> {
    const newCount = await this.page.$$eval('main article', els => els.length);
    return newCount > currentCount;
  }

  /**
   * Random delay to mimic human behavior
   */
  private randomDelay(min = 1000, max = 2500): Promise<void> {
    const delay = Math.floor(Math.random() * (max - min + 1)) + min;
    return new Promise(resolve => setTimeout(resolve, delay));
  }
}
