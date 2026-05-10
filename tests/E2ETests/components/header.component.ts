import { Locator, Page } from '@playwright/test';

export class HeaderComponent {
  readonly page: Page;
  readonly logo: Locator;
  readonly catalogLink: Locator;
  readonly sellLink: Locator;
  readonly loginLink: Locator;
  readonly registerLink: Locator;

  constructor(page: Page) {
    this.page = page;
    this.logo = page.getByTestId('header-logo');
    this.catalogLink = page.getByTestId('nav-catalog');
    this.sellLink = page.getByTestId('nav-sell');
    this.loginLink = page.getByTestId('nav-login');
    this.registerLink = page.getByTestId('nav-register');
  }

  async clickLogo() {
    await this.logo.click();
  }

  async clickCatalog() {
    await this.catalogLink.click();
  }

  async clickSell() {
    await this.sellLink.click();
  }

  async clickLogin() {
    await this.loginLink.click();
  }

  async clickRegister() {
    await this.registerLink.click();
  }
}
