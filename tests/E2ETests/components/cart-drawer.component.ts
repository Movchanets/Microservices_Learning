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
    this.drawer = page.locator('app-mini-cart');
    this.heading = page.getByRole('heading', { name: 'Shopping Cart' });
    this.closeBtn = this.drawer.locator('button').filter({ has: page.locator('lucide-icon[name="X"]') });
    this.emptyMessage = page.getByText('Your cart is empty');
    this.itemsList = this.drawer.locator('ul');
    this.items = this.itemsList.locator('li');
    this.totalText = this.drawer.locator('p.text-2xl.font-bold');
    this.viewCartLink = page.getByRole('link', { name: 'View Full Cart' });
    this.checkoutLink = page.getByRole('link', { name: 'Go to Checkout' });
  }

  async waitForOpen() {
    await expect(this.drawer).toBeVisible();
  }

  async waitForClose() {
    await expect(this.drawer).toBeHidden();
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
    await item.locator('button').filter({ has: this.page.locator('lucide-icon[name="Trash2"]') }).click();
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
