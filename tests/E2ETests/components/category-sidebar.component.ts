import { Locator, Page } from '@playwright/test';
import { BaseComponent } from './base.component';

/**
 * Component object for the category sidebar filter.
 */
export class CategorySidebarComponent extends BaseComponent {
  readonly heading: Locator;
  readonly allProductsBtn: Locator;
  readonly categoryButtons: Locator;

  constructor(page: Page) {
    const root = page.locator('app-category-sidebar');
    super(page, root);
    this.heading = this.root.getByRole('heading', { name: /categories/i });
    this.allProductsBtn = this.root.getByRole('button', { name: /all products/i });
    this.categoryButtons = this.root.locator('button:not(:first-child)');
  }

  async selectCategory(name: string) {
    const btn = this.root.getByRole('button', { name });
    await btn.click();
  }

  async selectAll() {
    await this.allProductsBtn.click();
  }

  async getCategoryNames(): Promise<string[]> {
    const buttons = this.root.locator('button');
    const texts = await buttons.allTextContents();
    return texts.map(t => t.trim()).filter(t => t.length > 0);
  }

  async getSelectedCategory(): Promise<string | null> {
    const activeBtn = this.root.locator('button.bg-primary, button[class*="bg-primary"]');
    if (await activeBtn.count() === 0) return null;
    return activeBtn.innerText();
  }
}
