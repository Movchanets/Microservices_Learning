import { Locator, Page } from '@playwright/test';
import { BaseComponent } from './base.component';
import { TIMEOUTS } from '../utils/constants';

/**
 * Component object for the slide-in cart drawer.
 *
 * Scoped to `<app-cart-drawer>`. The drawer content is conditionally rendered
 * via `@if (cartStore.isDrawerOpen())`, so locators only resolve when open.
 */
export class CartDrawerComponent extends BaseComponent {
  // ── Header ──────────────────────────────────────────────
  readonly heading: Locator;
  readonly closeBtn: Locator;

  // ── Content ─────────────────────────────────────────────
  readonly emptyMessage: Locator;
  readonly itemsList: Locator;
  readonly items: Locator;
  readonly totalText: Locator;

  // ── Footer Actions ──────────────────────────────────────
  readonly viewCartLink: Locator;
  readonly checkoutLink: Locator;

  constructor(page: Page) {
    const root = page.locator('app-cart-drawer');
    super(page, root);

    // Header
    this.heading = this.root.getByRole('heading', { name: /Your Cart/ });
    this.closeBtn = this.root.getByTestId('cart-drawer-close');

    // Content
    this.emptyMessage = this.root.getByText('Your cart is empty.');
    this.itemsList = this.root.locator('.cart-items');
    this.items = this.root.locator('.cart-item');
    this.totalText = this.root.getByTestId('cart-total');

    // Footer Actions
    this.viewCartLink = this.root.getByTestId('cart-drawer-view-cart');
    this.checkoutLink = this.root.getByTestId('cart-drawer-checkout');
  }

  // ── Lifecycle ───────────────────────────────────────────

  /** Wait for the drawer heading to become visible (drawer opened). */
  async waitForOpen() {
    await this.heading.waitFor({ state: 'visible', timeout: TIMEOUTS.element });
  }

  /** Wait for the drawer heading to disappear (drawer closed). */
  async waitForClose() {
    await this.heading.waitFor({ state: 'hidden', timeout: TIMEOUTS.quick });
  }

  /** Click the X button to close the drawer. */
  async close() {
    await this.closeBtn.click();
  }

  // ── Item Actions ────────────────────────────────────────

  /** Return the number of cart items currently visible. */
  async getItemCount(): Promise<number> {
    return this.items.count();
  }

  /** Find a cart item row by SKU text. */
  async getItemBySku(sku: string) {
    return this.items.filter({ hasText: sku });
  }

  /** Click the remove button on the item matching the given SKU. */
  async removeItem(sku: string) {
    const item = await this.getItemBySku(sku);
    await item.locator('.remove-btn').click();
  }

  // ── Queries ─────────────────────────────────────────────

  /** Read the subtotal text (e.g. "$129.99"). */
  async getTotal(): Promise<string> {
    return this.totalText.innerText();
  }

  /** Navigate to the full cart page. */
  async goToCart() {
    await this.viewCartLink.click();
  }

  /** Navigate to the checkout page. */
  async goToCheckout() {
    await this.checkoutLink.click();
  }

  /** True if the "Your cart is empty" message is visible. */
  async isEmpty(): Promise<boolean> {
    return this.emptyMessage.isVisible();
  }
}
