import { Locator, Page } from '@playwright/test';

export class CategorySidebarComponent {
  readonly page: Page;
  readonly container: Locator;
  readonly heading: Locator;
  readonly allProductsBtn: Locator;
  readonly categoryButtons: Locator;

  constructor(page: Page) {
    this.page = page;
    this.container = page.locator('app-category-sidebar');
    this.heading = this.container.getByRole('heading', { name: /categories/i });
    this.allProductsBtn = this.container.getByRole('button', { name: /all products/i });
    this.categoryButtons = this.container.locator('button:not(:first-child)');
  }

  async isVisible(): Promise<boolean> {
    return this.container.isVisible();
  }

  async selectCategory(name: string) {
    const btn = this.container.getByRole('button', { name });
    await btn.click();
  }

  async selectAll() {
    await this.allProductsBtn.click();
  }

  async getCategoryNames(): Promise<string[]> {
    const buttons = this.container.locator('button');
    const texts = await buttons.allTextContents();
    return texts.map(t => t.trim()).filter(t => t.length > 0);
  }

  async getSelectedCategory(): Promise<string | null> {
    const activeBtn = this.container.locator('button.bg-primary, button[class*="bg-primary"]');
    if (await activeBtn.count() === 0) return null;
    return activeBtn.innerText();
  }
}
