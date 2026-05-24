import { Locator, Page } from '@playwright/test';

export class SearchBarComponent {
  readonly page: Page;
  readonly searchInput: Locator;
  readonly searchBtn: Locator;

  constructor(page: Page) {
    this.page = page;
    this.searchInput = page.getByPlaceholder('Search products...');
    this.searchBtn = page.getByRole('button', { name: '' }).filter({ has: page.locator('lucide-icon[name="Search"]') });
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
