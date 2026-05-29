import { Locator, Page } from '@playwright/test';
import { BaseComponent } from './base.component';

export class SearchBarComponent extends BaseComponent {
  readonly searchInput: Locator;
  readonly searchBtn: Locator;

  constructor(page: Page) {
    const root = page.locator('app-search-bar');
    super(page, root);

    this.searchInput = this.root.getByPlaceholder('Search products...');
    this.searchBtn = this.root.getByRole('button', { name: '' }).filter({ has: this.root.locator('lucide-icon[name="Search"]') });
  }

  async search(query: string) {
    await this.searchInput.fill(query);
    await this.searchInput.press('Enter');
  }

  async typeAndSearch(query: string) {
    await this.searchInput.fill(query);
    await this.searchBtn.click();
  }

  async getValue(): Promise<string> {
    return this.searchInput.inputValue();
  }

  async clear() {
    await this.searchInput.clear();
  }
}
