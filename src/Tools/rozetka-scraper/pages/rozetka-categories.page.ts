/**
 * Rozetka Categories Page Object
 * 
 * Scrapes the category tree from Rozetka's main catalog page.
 * Categories are hierarchical: parent > child > subchild
 */

import { Page } from 'playwright';

export interface CategoryNode {
  name: string;
  url: string;
  id: string;            // Extracted from URL (e.g., c80004)
  parentId?: string;
  level: number;
  children: CategoryNode[];
}

export class RozetkaCategoriesPage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  /**
   * Navigate to the main catalog page
   */
  async goto(): Promise<void> {
    await this.page.goto('https://rozetka.com.ua/ua/', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await this.randomDelay(2000, 3000);
  }

  /**
   * Scrape top-level categories from the catalog menu
   */
  async scrapeTopCategories(): Promise<CategoryNode[]> {
    return this.page.evaluate(() => {
      const categories: Array<{
        name: string;
        url: string;
        id: string;
        level: number;
        children: any[];
      }> = [];

      // Rozetka catalog menu: look for category links
      const seen = new Set<string>();

      // Method 1: Main navigation menu
      document.querySelectorAll('[class*="menu"] a, [class*="catalog"] a, [class*="nav"] a').forEach(a => {
        const href = a.getAttribute('href') || '';
        const text = a.textContent?.trim() || '';

        // Match category URLs like /ua/computers-notebooks/c80253/
        const catMatch = href.match(/\/ua\/[^/]+\/(c\d+)\//);
        if (catMatch && text.length > 1 && text.length < 60 && !seen.has(catMatch[1])) {
          seen.add(catMatch[1]);
          categories.push({
            name: text,
            url: href.startsWith('http') ? href : `https://rozetka.com.ua${href}`,
            id: catMatch[1],
            level: 0,
            children: [],
          });
        }
      });

      // Method 2: Homepage category cards/tiles
      document.querySelectorAll('a[href*="/c"]').forEach(a => {
        const href = a.getAttribute('href') || '';
        const text = a.textContent?.trim() || '';
        const catMatch = href.match(/\/ua\/[^/]+\/(c\d+)\//);

        if (catMatch && text.length > 1 && text.length < 60 && !seen.has(catMatch[1])) {
          seen.add(catMatch[1]);
          categories.push({
            name: text,
            url: href.startsWith('http') ? href : `https://rozetka.com.ua${href}`,
            id: catMatch[1],
            level: 0,
            children: [],
          });
        }
      });

      return categories;
    });
  }

  /**
   * Scrape subcategories from a category page
   */
  async scrapeSubcategories(categoryUrl: string): Promise<CategoryNode[]> {
    await this.page.goto(categoryUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
    await this.randomDelay(1500, 2500);

    return this.page.evaluate((baseUrl) => {
      const subcategories: Array<{
        name: string;
        url: string;
        id: string;
        level: number;
        children: any[];
      }> = [];

      const seen = new Set<string>();

      // Look for subcategory links in the page
      document.querySelectorAll('a[href]').forEach(a => {
        const href = a.getAttribute('href') || '';
        const text = a.textContent?.trim() || '';

        // Match subcategory URLs
        const catMatch = href.match(/\/ua\/[^/]+\/(c\d+)\//);
        if (catMatch && catMatch[1] !== baseUrl.match(/\/(c\d+)\//)?.[1]) {
          if (text.length > 1 && text.length < 60 && !seen.has(catMatch[1])) {
            seen.add(catMatch[1]);
            subcategories.push({
              name: text,
              url: href.startsWith('http') ? href : `https://rozetka.com.ua${href}`,
              id: catMatch[1],
              level: 1,
              children: [],
            });
          }
        }
      });

      // Also look for filter/tag links that represent subcategories
      document.querySelectorAll('[class*="filter"] a, [class*="tag"] a, [class*="chip"] a').forEach(a => {
        const href = a.getAttribute('href') || '';
        const text = a.textContent?.trim() || '';

        if (href.includes(baseUrl.replace('https://rozetka.com.ua', '')) && text.length > 1 && text.length < 50) {
          const fullUrl = href.startsWith('http') ? href : `https://rozetka.com.ua${href}`;
          if (!seen.has(fullUrl)) {
            seen.add(fullUrl);
            subcategories.push({
              name: text,
              url: fullUrl,
              id: fullUrl.split('/').pop()?.replace(';', '_') || '',
              level: 1,
              children: [],
            });
          }
        }
      });

      return subcategories;
    }, categoryUrl);
  }

  /**
   * Build a category tree by scraping multiple levels
   */
  async buildCategoryTree(maxDepth = 2): Promise<CategoryNode[]> {
    const topCategories = await this.scrapeTopCategories();

    if (maxDepth < 1) return topCategories;

    // Scrape subcategories for each top-level category
    for (const cat of topCategories.slice(0, 10)) { // Limit to avoid too many requests
      try {
        cat.children = await this.scrapeSubcategories(cat.url);
        await this.randomDelay(1000, 2000);
      } catch {
        // Skip failed categories
      }
    }

    return topCategories;
  }

  /**
   * Export category tree as flat list with parent references
   */
  flattenTree(tree: CategoryNode[]): Array<{ name: string; url: string; id: string; parentId?: string; level: number }> {
    const result: Array<{ name: string; url: string; id: string; parentId?: string; level: number }> = [];

    const walk = (nodes: CategoryNode[], parentId?: string) => {
      for (const node of nodes) {
        result.push({
          name: node.name,
          url: node.url,
          id: node.id,
          parentId,
          level: node.level,
        });
        if (node.children.length > 0) {
          walk(node.children, node.id);
        }
      }
    };

    walk(tree);
    return result;
  }

  private randomDelay(min = 1000, max = 2500): Promise<void> {
    const delay = Math.floor(Math.random() * (max - min + 1)) + min;
    return new Promise(resolve => setTimeout(resolve, delay));
  }
}
