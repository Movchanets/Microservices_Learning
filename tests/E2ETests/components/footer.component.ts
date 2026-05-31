import { Locator, Page } from '@playwright/test';
import { BaseComponent } from './base.component';

/**
 * Component object for the site footer.
 */
export class FooterComponent extends BaseComponent {
  readonly themeToggle: Locator;
  readonly langToggle: Locator;

  constructor(page: Page) {
    const root = page.locator('footer');
    super(page, root);

    this.themeToggle = this.root.getByTestId('theme-toggle-btn');
    this.langToggle = this.root.getByTestId('lang-toggle-btn');
  }

  async toggleTheme() {
    await this.themeToggle.click();
  }

  async toggleLanguage() {
    await this.langToggle.click();
  }
}
