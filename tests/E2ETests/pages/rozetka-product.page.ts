/**
 * Rozetka Product Detail Page Object
 * 
 * Encapsulates interactions with Rozetka product detail pages.
 * Handles description extraction and image gallery parsing.
 */

import { Page } from '@playwright/test';

export interface ProductDetails {
  description: string;
  images: string[];
}

export class RozetkaProductPage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  /**
   * Navigate to a product page
   */
  async goto(url: string): Promise<void> {
    await this.page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
    await this.randomDelay(1500, 2500);
  }

  /**
   * Extract product description and image gallery
   */
  async extractDetails(): Promise<ProductDetails> {
    return this.page.evaluate(() => {
      // Description
      const descEl = document.querySelector(
        '[class*="product-about"] p, [class*="about__brief"], [class*="description"] p'
      );
      const description = descEl?.textContent?.trim() || '';

      // Images
      const images: string[] = [];

      // Main product images
      document.querySelectorAll('img[src*="rozetka.com.ua/goods"]').forEach(img => {
        const src = img.getAttribute('src') || '';
        if (src && !src.includes('tag') && !src.includes('preview')) {
          images.push(src);
        }
      });

      // Lazy-loaded images
      document.querySelectorAll('img[data-src*="rozetka.com.ua/goods"]').forEach(img => {
        const src = img.getAttribute('data-src') || '';
        if (src && !images.includes(src)) {
          images.push(src);
        }
      });

      return { description, images: images.slice(0, 8) };
    });
  }

  /**
   * Random delay
   */
  private randomDelay(min = 1000, max = 2500): Promise<void> {
    const delay = Math.floor(Math.random() * (max - min + 1)) + min;
    return new Promise(resolve => setTimeout(resolve, delay));
  }
}
