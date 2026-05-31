/**
 * Rozetka Category Page Object
 * 
 * Handles product listing extraction and pagination from Rozetka category pages.
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

  constructor(page: Page) {
    this.page = page;
  }

  async goto(url: string): Promise<void> {
    await this.page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await this.page.waitForSelector('main article', { timeout: 30000 });
    await this.randomDelay(1500, 2500);
  }

  /**
   * Extract all product tiles from current listing page
   */
  async extractProductTiles(): Promise<ProductTile[]> {
    return this.page.evaluate(() => {
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

        // Price text
        const text = article.textContent || '';
        const priceMatch = text.match(/(\d[\d\s]*₴[\d\s₴]*)/);
        const priceText = priceMatch ? priceMatch[1] : '';

        // Brand from title
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
   * Navigate to next page
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
