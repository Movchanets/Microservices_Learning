import { Locator, Page } from '@playwright/test';
import { BaseComponent } from './base.component';

/**
 * Component object for the pagination nav.
 *
 * Scoped to `<nav aria-label="Pagination">`. Provides page navigation,
 * prev/next buttons, and current page detection.
 */
export class PaginationComponent extends BaseComponent {
  readonly prevBtn: Locator;
  readonly nextBtn: Locator;
  readonly pageButtons: Locator;

  constructor(page: Page) {
    const root = page.locator('nav[aria-label="Pagination"]');
    super(page, root);
    this.prevBtn = this.root.locator('button').first();
    this.nextBtn = this.root.locator('button').last();
    this.pageButtons = this.root.locator('button:not(:first-child):not(:last-child)');
  }

  async goToPage(n: number) {
    const btn = this.root.locator('button', { hasText: String(n) });
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
    const activeBtn = this.root.locator('button.bg-primary, button[class*="bg-primary"]');
    const text = await activeBtn.innerText();
    return parseInt(text, 10);
  }
}
