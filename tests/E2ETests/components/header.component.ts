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
  readonly adminLink: Locator;

  // Mega menu
  readonly megaMenu: Locator;
  readonly megaMenuToggle: Locator;

  // Search
  readonly searchInput: Locator;

  // Cart
  readonly cartBtn: Locator;
  readonly cartBadge: Locator;

  constructor(page: Page) {
    this.page = page;
    this.logo = page.getByTestId('header-logo');
    this.catalogLink = page.getByRole('button', { name: /catalog/i }).first();
    this.sellLink = page.getByRole('link', { name: /sell/i });
    this.loginLink = page.getByTestId('nav-login');
    this.registerLink = page.getByTestId('nav-register');
    this.userMenuTrigger = page.getByTestId('user-menu-trigger');
    this.userDropdown = page.getByTestId('user-dropdown');
    this.profileLink = page.getByTestId('nav-profile');
    this.logoutLink = page.getByTestId('nav-logout');
    this.adminLink = page.getByTestId('nav-admin');

    // Mega menu — target the visible panel div, not the host element (which has 0 dimensions)
    this.megaMenu = page.getByTestId('mega-menu-panel');
    this.megaMenuToggle = page.getByRole('button', { name: /catalog/i }).first();

    // Search — scope to header to avoid matching standalone search bar on catalog page
    this.searchInput = page.locator('header').getByPlaceholder('Search products...');

    // Cart — scope to header only to avoid matching product card buttons
    this.cartBtn = page.getByTestId('cart-button');
    this.cartBadge = page.getByTestId('cart-badge');
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

  async clickAdmin() {
    await this.openUserMenu();
    await this.adminLink.click();
  }

  // Mega menu
  async toggleMegaMenu() {
    await this.megaMenuToggle.click();
  }

  async isMegaMenuOpen(): Promise<boolean> {
    return this.megaMenu.isVisible();
  }

  async closeMegaMenu() {
    // Click elsewhere to close
    await this.page.locator('body').click({ position: { x: 0, y: 0 } });
  }

  // Search
  async search(query: string) {
    await this.searchInput.fill(query);
    await this.searchInput.press('Enter');
  }

  async typeSearch(query: string) {
    await this.searchInput.fill(query);
  }

  // Cart
  async openCart() {
    await this.cartBtn.click();
  }

  async getCartBadgeCount(): Promise<string | null> {
    if (await this.cartBadge.isVisible()) {
      return this.cartBadge.innerText();
    }
    return null;
  }

  async hasCartBadge(): Promise<boolean> {
    return this.cartBadge.isVisible();
  }

  // User state checks
  async isLoggedIn(): Promise<boolean> {
    return this.userMenuTrigger.isVisible();
  }

  async isLoggedOut(): Promise<boolean> {
    return this.loginLink.isVisible();
  }

  async getUserEmail(): Promise<string> {
    await this.openUserMenu();
    const email = this.page.locator('[class*="text-xs"][class*="text-muted"]').filter({ hasText: /@/ });
    return email.innerText();
  }
}
