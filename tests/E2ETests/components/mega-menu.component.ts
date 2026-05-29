import { Locator, Page } from '@playwright/test';
import { BaseComponent } from './base.component';

/**
 * Component object for the mega menu dropdown.
 */
export class MegaMenuComponent extends BaseComponent {
  readonly rootCategories: Locator;
  readonly subcategoriesPanel: Locator;
  readonly categoryLinks: Locator;

  constructor(page: Page) {
    const root = page.getByTestId('mega-menu-panel');
    super(page, root);

    this.rootCategories = this.root.getByTestId('root-category-btn');
    this.subcategoriesPanel = this.root.locator('.flex-1.p-8');
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
