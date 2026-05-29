import { Page, Locator } from '@playwright/test';

/**
 * Abstract base for all Component Objects.
 *
 * Every component MUST:
 *   1. Pass a `root` locator to `super(page, root)` — the host element
 *   2. Scope ALL child locators to `this.root`, never `this.page`
 *
 * Provides common helpers: `isVisible()`, `waitForReady()`.
 */
export abstract class BaseComponent {
  readonly page: Page;
  readonly root: Locator;

  constructor(page: Page, root: Locator) {
    this.page = page;
    this.root = root;
  }

  /** Whether the component's root element is visible. */
  async isVisible(): Promise<boolean> {
    return this.root.isVisible();
  }

  /** Wait for the component to appear in the DOM. */
  async waitForReady(timeout?: number): Promise<void> {
    await this.root.waitFor({ state: 'visible', timeout });
  }
}
