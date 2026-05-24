import { Locator, Page } from '@playwright/test';

export class PaginationComponent {
  readonly page: Page;
  readonly container: Locator;
  readonly prevBtn: Locator;
  readonly nextBtn: Locator;
  readonly pageButtons: Locator;

  constructor(page: Page) {
    this.page = page;
    this.container = page.locator('nav[aria-label="Pagination"]');
    this.prevBtn = this.container.locator('button').first();
    this.nextBtn = this.container.locator('button').last();
    this.pageButtons = this.container.locator('button:not(:first-child):not(:last-child)');
  }

  async isVisible(): Promise<boolean> {
    return this.container.isVisible();
  }

  async goToPage(n: number) {
    const btn = this.container.locator('button', { hasText: String(n) });
    await btn.click();
  }

  async next() {
    await this.nextBtn.click();
  }

  async previous() {
    await this.prevBtn.click();
  }

  async hasNext(): Promise<boolean> {
    return !(await this.nextBtn.isDisabled());
  }

  async hasPrevious(): Promise<boolean> {
    return !(await this.prevBtn.isDisabled());
  }

  async getCurrentPage(): Promise<number> {
    const activeBtn = this.container.locator('button.bg-primary, button[class*="bg-primary"]');
    const text = await activeBtn.innerText();
    return parseInt(text, 10);
  }
}
