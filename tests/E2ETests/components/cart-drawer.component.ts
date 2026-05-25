import { Locator, Page, expect } from '@playwright/test';

export class CartDrawerComponent {
  readonly page: Page;
  readonly drawer: Locator;
  readonly heading: Locator;
  readonly closeBtn: Locator;
  readonly emptyMessage: Locator;
  readonly itemsList: Locator;
  readonly items: Locator;
  readonly totalText: Locator;
  readonly viewCartLink: Locator;
  readonly checkoutLink: Locator;

  constructor(page: Page) {
    this.page = page;
    this.drawer = page.locator('app-cart-drawer');
    this.heading = page.getByRole('heading', { name: /Your Cart/ });
    this.closeBtn = page.getByTestId('cart-drawer-close');
    this.emptyMessage = page.getByText('Your cart is empty.');
    this.itemsList = this.drawer.locator('.cart-items');
    this.items = this.drawer.locator('.cart-item');
    this.totalText = page.getByTestId('cart-total');
    this.viewCartLink = page.getByTestId('cart-drawer-view-cart');
    this.checkoutLink = page.getByTestId('cart-drawer-checkout');
  }

  async waitForOpen() {
    // The drawer overlay appears when isDrawerOpen() is true
    await expect(this.heading).toBeVisible({ timeout: 10000 });
  }

  async waitForClose() {
    await expect(this.heading).toBeHidden({ timeout: 5000 });
  }

  async close() {
    await this.closeBtn.click();
  }

  async getItemCount(): Promise<number> {
    return this.items.count();
  }

  async getItemBySku(sku: string): Promise<Locator> {
    return this.items.filter({ hasText: sku });
  }

  async removeItem(sku: string) {
    const item = await this.getItemBySku(sku);
    await item.locator('.remove-btn').click();
  }

  async getTotal(): Promise<string> {
    return this.totalText.innerText();
  }

  async goToCart() {
    await this.viewCartLink.click();
  }

  async goToCheckout() {
    await this.checkoutLink.click();
  }

  async isEmpty(): Promise<boolean> {
    return this.emptyMessage.isVisible();
  }
}
