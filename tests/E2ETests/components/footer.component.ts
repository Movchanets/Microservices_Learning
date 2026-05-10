import { Locator, Page } from '@playwright/test';

export class FooterComponent {
  readonly page: Page;
  readonly themeToggle: Locator;
  readonly langToggle: Locator;

  constructor(page: Page) {
    this.page = page;
    this.themeToggle = page.getByTestId('theme-toggle-btn');
    this.langToggle = page.getByTestId('lang-toggle-btn');
  }

  async toggleTheme() {
    await this.themeToggle.click();
  }

  async toggleLanguage() {
    await this.langToggle.click();
  }
}
