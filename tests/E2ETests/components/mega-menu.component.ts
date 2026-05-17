import { Locator, Page } from '@playwright/test';

export class MegaMenuComponent {
  readonly page: Page;
  readonly rootCategories: Locator;
  readonly subcategoriesPanel: Locator;
  readonly categoryLinks: Locator;

  constructor(page: Page) {
    this.page = page;
    this.rootCategories = page.locator('.w-1\\/4 button');
    this.subcategoriesPanel = page.locator('.flex-1.p-8');
    this.categoryLinks = this.subcategoriesPanel.locator('a');
  }

  async getRootCategoryNames(): Promise<string[]> {
    return this.rootCategories.allTextContents();
  }

  async hoverRootCategory(name: string) {
    await this.rootCategories.filter({ hasText: name }).hover();
  }

  async clickCategory(name: string) {
    await this.categoryLinks.filter({ hasText: name }).first().click();
  }

  async getVisibleSubcategories(): Promise<string[]> {
    return this.subcategoriesPanel.locator('h3, a').allTextContents();
  }

  async isVisible(): Promise<boolean> {
    return this.subcategoriesPanel.isVisible();
  }
}
