import { Locator, Page } from '@playwright/test';
import { BaseComponent } from './base.component';

/**
 * Component object for the site header.
 *
 * Scoped to `<header>` — all locators use `this.root`, not `this.page`.
 * Covers: logo, nav links, user menu, mega menu, search, cart.
 */
export class HeaderComponent extends BaseComponent {
  // ── Branding ────────────────────────────────────────────
  readonly logo: Locator;

  // ── Navigation Links ────────────────────────────────────
  readonly catalogLink: Locator;
  readonly loginLink: Locator;
  readonly registerLink: Locator;

  // ── User Menu (authenticated) ───────────────────────────
  readonly userMenuTrigger: Locator;
  readonly userDropdown: Locator;
  readonly profileLink: Locator;
  readonly logoutLink: Locator;
  readonly ordersLink: Locator;
  readonly sellerLink: Locator;
  readonly adminLink: Locator;

  // ── Mega Menu ───────────────────────────────────────────
  readonly megaMenu: Locator;
  readonly megaMenuToggle: Locator;

  // ── Search ──────────────────────────────────────────────
  readonly searchInput: Locator;

  // ── Cart ────────────────────────────────────────────────
  readonly cartBtn: Locator;
  readonly cartBadge: Locator;

  constructor(page: Page) {
    const root = page.locator('header');
    super(page, root);

    // Branding
    this.logo = this.root.getByTestId('header-logo');

    // Navigation
    this.catalogLink = this.root.getByRole('button', { name: /catalog/i }).first();
    this.loginLink = this.root.getByTestId('nav-login');
    this.registerLink = this.root.getByTestId('nav-register');

    // User Menu
    this.userMenuTrigger = this.root.getByTestId('user-menu-trigger');
    this.userDropdown = this.root.getByTestId('user-dropdown');
    this.profileLink = this.root.getByTestId('nav-profile');
    this.logoutLink = this.root.getByTestId('nav-logout');
    this.ordersLink = this.root.getByTestId('nav-orders');
    this.sellerLink = this.root.getByTestId('nav-seller');
    this.adminLink = this.root.getByTestId('nav-admin');

    // Mega Menu — target the visible panel div, not the host element (which has 0 dimensions)
    // NOTE: Uses this.page because the mega menu panel renders outside <header> in the DOM
    this.megaMenu = this.page.getByTestId('mega-menu-panel'); // eslint-disable-line @typescript-eslint/no-this-alias -- panel is outside header scope
    this.megaMenuToggle = this.root.getByRole('button', { name: /catalog/i }).first();

    // Search — scope to header to avoid matching standalone search bar on catalog page
    this.searchInput = this.root.getByPlaceholder('Search products...');

    // Cart — scope to header only to avoid matching product card buttons
    this.cartBtn = this.root.getByTestId('cart-button');
    this.cartBadge = this.root.getByTestId('cart-badge');
  }

  // ── Navigation Actions ──────────────────────────────────

  async clickLogo() {
    await this.logo.click();
  }

  async clickCatalog() {
    await this.catalogLink.click();
  }

  async clickLogin() {
    await this.loginLink.click();
  }

  async clickRegister() {
    await this.registerLink.click();
  }

  // ── User Menu Actions ───────────────────────────────────

  /** Open the user dropdown menu. */
  async openUserMenu() {
    await this.userMenuTrigger.click();
  }

  /** Open user menu, then click "Profile". */
  async clickProfile() {
    await this.openUserMenu();
    await this.profileLink.click();
  }

  /** Open user menu, then click "Logout". */
  async logout() {
    await this.openUserMenu();
    await this.logoutLink.click();
  }

  /** Open user menu, then click "Admin". */
  async clickAdmin() {
    await this.openUserMenu();
    await this.adminLink.click();
  }

  /** Open user menu, then click "Seller Dashboard". */
  async clickSellerDashboard() {
    await this.openUserMenu();
    await this.sellerLink.click();
  }

  /** Open user menu, then click "Orders". */
  async clickOrders() {
    await this.openUserMenu();
    await this.ordersLink.click();
  }

  // ── Mega Menu ───────────────────────────────────────────

  async toggleMegaMenu() {
    await this.megaMenuToggle.click();
  }

  async isMegaMenuOpen(): Promise<boolean> {
    return this.megaMenu.isVisible();
  }

  /** Click the page body to dismiss the mega menu. */
  async closeMegaMenu() {
    await this.page.locator('body').click({ position: { x: 0, y: 0 } });
  }

  // ── Search ──────────────────────────────────────────────

  /** Type a query and press Enter. */
  async search(query: string) {
    await this.searchInput.fill(query);
    await this.searchInput.press('Enter');
  }

  /** Type a query without submitting (for autocomplete testing). */
  async typeSearch(query: string) {
    await this.searchInput.fill(query);
  }

  // ── Cart ────────────────────────────────────────────────

  /** Click the cart icon to open the drawer. */
  async openCart() {
    await this.cartBtn.click();
  }

  /** Return the badge count text, or null if badge is hidden. */
  async getCartBadgeCount(): Promise<string | null> {
    if (await this.cartBadge.isVisible()) {
      return this.cartBadge.innerText();
    }
    return null;
  }

  async hasCartBadge(): Promise<boolean> {
    return this.cartBadge.isVisible();
  }

  // ── State Checks ────────────────────────────────────────

  /** True if the user menu trigger is visible (authenticated). */
  async isLoggedIn(): Promise<boolean> {
    return this.userMenuTrigger.isVisible();
  }

  /** True if the login link is visible (anonymous). */
  async isLoggedOut(): Promise<boolean> {
    return this.loginLink.isVisible();
  }

  /** Open user menu and extract the email text. */
  async getUserEmail(): Promise<string> {
    await this.openUserMenu();
    const email = this.root.locator('[class*="text-xs"][class*="text-muted"]').filter({ hasText: /@/ });
    return email.innerText();
  }
}
