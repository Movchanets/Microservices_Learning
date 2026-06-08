import { Page, Locator } from 'playwright';

export interface CatalogCategory {
  name: string;
  url: string;
  id: string; // extracted from URL, e.g., c4625734
}

export interface CatalogSubcategory {
  name: string;
  url: string;
  id: string; // extracted from URL
  parentCategoryName: string;
}

/**
 * Component Object Model representing Rozetka's Catalog Menu.
 * Handles opening/closing, hovering/selecting categories, and extracting hierarchical links.
 */
export class RozetkaCatalogComponent {
  readonly page: Page;
  readonly triggerButton: Locator;
  readonly menuContainer: Locator;
  readonly sidebarLinks: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Toggle button in the main header (marked with fat_menu_btn test ID)
    this.triggerButton = page.locator('button[data-testid="fat_menu_btn"], button:has-text("Каталог")').first();
    
    // The open flyout catalog menu container (.layout child has non-zero size for visibility checks)
    this.menuContainer = page.locator('rz-fat-menu .layout');
    
    // Links to top-level categories inside the sidebar of the catalog menu
    this.sidebarLinks = this.menuContainer.locator('a.category-link');
  }

  /**
   * Opens the catalog menu if it is not already visible.
   */
  async open(): Promise<this> {
    let retries = 3;
    while (retries > 0) {
      const isVisible = await this.menuContainer.isVisible();
      if (isVisible) break;

      console.log(`Clicking catalog trigger button... (retries left: ${retries})`);
      await this.triggerButton.click({ force: true });
      
      try {
        // Wait a short time to see if the click was successful and opened the menu
        await this.menuContainer.waitFor({ state: 'visible', timeout: 5000 });
        break;
      } catch (e) {
        console.log('Menu container did not become visible, waiting before retry...');
        await this.page.waitForTimeout(2000);
        retries--;
      }
    }
    
    // Final check to guarantee visibility
    await this.menuContainer.waitFor({ state: 'visible', timeout: 5000 });
    return this;
  }

  /**
   * Closes the catalog menu if it is currently visible.
   */
  async close(): Promise<this> {
    const isVisible = await this.menuContainer.isVisible();
    if (isVisible) {
      console.log('Closing catalog menu...');
      await this.triggerButton.click({ force: true });
      await this.menuContainer.waitFor({ state: 'hidden', timeout: 15000 });
    }
    return this;
  }

  /**
   * Get the sidebar category link locator by its exact or partial name.
   */
  getCategoryLocator(name: string): Locator {
    return this.sidebarLinks.filter({ hasText: name }).first();
  }

  /**
   * Hover over a top-level category in the sidebar to activate its subcategories panel.
   */
  async hoverCategory(name: string): Promise<this> {
    await this.open();
    const item = this.getCategoryLocator(name);
    
    console.log(`Hovering over category: "${name}"`);
    await item.scrollIntoViewIfNeeded();
    await item.hover();
    
    // Short delay for the dynamic subcategory panel to render in Angular
    await this.page.waitForTimeout(1000);
    return this;
  }

  /**
   * Scrapes all top-level categories from the open catalog menu.
   */
  async extractTopCategories(): Promise<CatalogCategory[]> {
    await this.open();
    
    return this.page.evaluate(() => {
      const links = Array.from(document.querySelectorAll('rz-fat-menu a.category-link'));
      return links.map(a => {
        const url = a.getAttribute('href') || '';
        const idMatch = url.match(/\/(c\d+)\//);
        return {
          name: a.textContent?.trim() || '',
          url: url.startsWith('http') ? url : `https://rozetka.com.ua${url}`,
          id: idMatch ? idMatch[1] : ''
        };
      }).filter(c => c.name && c.url);
    });
  }

  /**
   * Scrapes subcategories under a specific parent category.
   * Automatically opens the menu and hovers over the category before extracting.
   */
  async extractSubcategories(parentCategoryName: string): Promise<CatalogSubcategory[]> {
    await this.hoverCategory(parentCategoryName);

    return this.page.evaluate((parentName) => {
      const activePanel = document.querySelector('rz-fat-menu');
      if (!activePanel) return [];

      // Find all links in the panel
      const allLinks = Array.from(activePanel.querySelectorAll('a[href]'));
      
      // Filter out sidebar links (which have class 'category-link')
      const subLinks = allLinks.filter(a => !a.classList.contains('category-link'));

      return subLinks.map(a => {
        const url = a.getAttribute('href') || '';
        const idMatch = url.match(/\/(c\d+)\//) || url.match(/\/c(\d+)\//);
        return {
          name: a.textContent?.trim() || '',
          url: url.startsWith('http') ? url : `https://rozetka.com.ua${url}`,
          id: idMatch ? `c${idMatch[1]}` : '',
          parentCategoryName: parentName
        };
      }).filter(sub => sub.name && sub.url);
    }, parentCategoryName);
  }
}
