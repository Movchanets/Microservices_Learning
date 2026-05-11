import { Locator, Page } from '@playwright/test';

export class HeaderComponent {
  readonly page: Page;
  readonly logo: Locator;
  readonly catalogLink: Locator;
  readonly sellLink: Locator;
  readonly loginLink: Locator;
  readonly registerLink: Locator;
  readonly userMenuTrigger: Locator;
  readonly userDropdown: Locator;
  readonly profileLink: Locator;
  readonly logoutLink: Locator;

  constructor(page: Page) {
    this.page = page;
    this.logo = page.getByTestId('header-logo');
    this.catalogLink = page.getByTestId('nav-catalog');
    this.sellLink = page.getByTestId('nav-sell');
    this.loginLink = page.getByTestId('nav-login');
    this.registerLink = page.getByTestId('nav-register');
    this.userMenuTrigger = page.getByTestId('user-menu-trigger');
    this.userDropdown = page.getByTestId('user-dropdown');
    this.profileLink = page.getByTestId('nav-profile');
    this.logoutLink = page.getByTestId('nav-logout');
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

  async openUserMenu() {
    await this.userMenuTrigger.click();
  }

  async clickProfile() {
    await this.openUserMenu();
    await this.profileLink.click();
  }

  async logout() {
    await this.openUserMenu();
    await this.logoutLink.click();
  }
}
