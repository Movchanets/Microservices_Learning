/**
 * Rozetka Categories Page Object
 * 
 * Scrapes the category tree from Rozetka's main catalog page.
 * Categories are hierarchical: parent > child > subchild
 */

import { Page } from 'playwright';
import { mapFilterNameToKey } from '../utils/rozetka-transformer';

export interface CategoryNode {
  name: string;
  url: string;
  id: string;            // Extracted from URL (e.g., c80004)
  parentId?: string;
  ParentCategoryName?: string;
  level: number;
  children: CategoryNode[];
  AttributeDefinitions?: any[];
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
   * Build a category tree by scraping multiple levels.
   * Dynamically visits each subcategory page under fresh stealth contexts to extract its sidebar filters
   * and build dynamic AttributeDefinitions mapping.
   */
  async buildCategoryTree(maxDepth = 2): Promise<CategoryNode[]> {
    const topCategories = await this.scrapeTopCategories();

    if (maxDepth < 1) return topCategories;

    // Scrape subcategories for the first 4 top-level categories
    for (const cat of topCategories.slice(0, 4)) {
      const browser = this.page.context().browser()!;
      
      // Step 1: Navigating to the top portal page to get its subcategory links
      const ctx1 = await createStealthContext(browser);
      const page1 = await ctx1.newPage();
      const catPage1 = new RozetkaCategoriesPage(page1);
      
      let rawSubcategories: CategoryNode[] = [];
      try {
        rawSubcategories = await catPage1.scrapeSubcategories(cat.url);
      } catch (e) {
        console.error(`⚠️ Failed to scrape subcategories list for ${cat.name}:`, e);
      } finally {
        await page1.close();
        await ctx1.close();
      }

      // Limit to first 4 subcategories to stay extremely fast, focused, and block-free
      cat.children = rawSubcategories.slice(0, 4);

      // Step 2: Visit each subcategory's page to dynamically extract its real live sidebar filters!
      for (const child of cat.children) {
        const ctx2 = await createStealthContext(browser);
        const page2 = await ctx2.newPage();
        try {
          console.log(`🔍 Dynamically scraping filters for subcategory: ${child.name} (${child.url})`);
          await page2.goto(child.url, { waitUntil: 'domcontentloaded', timeout: 30000 });
          
          // Wait for dynamic filters to hydrate in the DOM
          try {
            await page2.waitForSelector('details[data-testid="filter"]', { timeout: 10000 });
          } catch {}
          
          await this.randomDelay(1000, 2000);

          // Scrape filters from the page DOM sidebar
          const filters = await page2.evaluate(() => {
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

          console.log(`   Found live filters:`, filters);

          child.ParentCategoryName = cat.name;
          
          // Map filters to AttributeDefinitions dynamically
          const attrDefs = filters.map((fName: string, idx: number) => {
            const key = mapFilterNameToKey(fName);
            const isVariant = isVariantAxisFilter(fName);
            return {
              Key: key,
              DisplayName: fName,
              Target: isVariant ? 1 : 0,           // 0 = Product, 1 = Sku
              ValueType: isSelectValueFilter(fName) ? 2 : 0, // 0 = Text, 2 = Select
              IsFilterable: true,
              IsRequired: false,
              IsVariantAxis: isVariant,
              SortOrder: idx + 1
            };
          });

          // Always guarantee Brand is included
          if (!attrDefs.some((a: any) => a.Key === 'brand')) {
            attrDefs.unshift({
              Key: 'brand',
              DisplayName: 'Бренд',
              Target: 0,
              ValueType: 0,
              IsFilterable: true,
              IsRequired: true,
              IsVariantAxis: false,
              SortOrder: 1
            });
            // Re-index SortOrder
            attrDefs.forEach((a: any, i: number) => a.SortOrder = i + 1);
          }

          child.AttributeDefinitions = attrDefs;

        } catch (e) {
          console.error(`⚠️ Failed to scrape live filters for ${child.name}:`, e);
          // Standard fallback
          child.ParentCategoryName = cat.name;
          child.AttributeDefinitions = [
            { Key: 'brand', DisplayName: 'Бренд', Target: 0, ValueType: 0, IsFilterable: true, IsRequired: true, SortOrder: 1 },
            { Key: 'color', DisplayName: 'Колір', Target: 1, ValueType: 2, IsFilterable: true, IsRequired: true, IsVariantAxis: true, SortOrder: 2 }
          ];
        } finally {
          await page2.close();
          await ctx2.close();
        }
      }
    }

    return topCategories;
  }

  /**
   * Export category tree as flat list with parent references and AttributeDefinitions.
   */
  flattenTree(tree: CategoryNode[]): Array<{
    name: string;
    url: string;
    id: string;
    parentId?: string;
    ParentCategoryName?: string;
    level: number;
    AttributeDefinitions?: any[];
  }> {
    const result: Array<{
      name: string;
      url: string;
      id: string;
      parentId?: string;
      ParentCategoryName?: string;
      level: number;
      AttributeDefinitions?: any[];
    }> = [];

    const walk = (nodes: CategoryNode[], parentId?: string, ParentCategoryName?: string) => {
      for (const node of nodes) {
        result.push({
          name: node.name,
          url: node.url,
          id: node.id,
          parentId,
          ParentCategoryName: node.ParentCategoryName || ParentCategoryName,
          level: node.level,
          AttributeDefinitions: node.AttributeDefinitions,
        });
        if (node.children.length > 0) {
          walk(node.children, node.id, node.name);
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

// ── Helpers for Dynamic Filtering & Translation ──────────────────

async function createStealthContext(browser: any) {
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36',
    viewport: { width: 1920, height: 1080 },
    locale: 'uk-UA',
    timezoneId: 'Europe/Kiev',
    extraHTTPHeaders: {
      'Accept-Language': 'uk-UA,uk;q=0.9,en;q=0.7',
    },
  });
  await ctx.addInitScript(() => {
    Object.defineProperty(navigator, 'webdriver', { get: () => false });
  });
  return ctx;
}

function isVariantAxisFilter(fName: string): boolean {
  const lower = fName.toLowerCase();
  return lower.includes('колір') || 
         lower.includes('color') || 
         lower.includes('пам\'ять') || 
         lower.includes('пам’ять') || 
         lower.includes('ram') || 
         lower.includes('озп') || 
         lower.includes('розмір') || 
         lower.includes('size');
}

function isSelectValueFilter(fName: string): boolean {
  const lower = fName.toLowerCase();
  return lower.includes('виробник') || 
         lower.includes('бренд') || 
         lower.includes('колір') || 
         lower.includes('пам\'ять') || 
         lower.includes('пам’ять') || 
         lower.includes('ram') || 
         lower.includes('екран') || 
         lower.includes('процесор') || 
         lower.includes('відеокарт');
}
