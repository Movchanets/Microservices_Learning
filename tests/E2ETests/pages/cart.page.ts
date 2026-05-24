import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

export class CartPage extends BasePage {
  readonly pageHeading: Locator;
  readonly emptyCartMessage: Locator;
  readonly continueShoppingBtn: Locator;
  readonly cartItems: Locator;
  readonly totalItemsCount: Locator;
  readonly checkoutBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByTestId('cart-heading');
    this.emptyCartMessage = page.getByTestId('cart-empty');
    this.continueShoppingBtn = page.getByTestId('cart-continue-shopping');
    this.cartItems = page.locator('[data-testid^="cart-item-"]').filter({ hasNot: page.locator('[data-testid^="cart-item-"][data-testid$="-increase"], [data-testid^="cart-item-"][data-testid$="-decrease"], [data-testid^="cart-item-"][data-testid$="-quantity"], [data-testid^="cart-item-"][data-testid$="-remove"]') });
    this.totalItemsCount = page.getByTestId('cart-total-items');
    this.checkoutBtn = page.getByTestId('cart-checkout-btn');
  }

  async goto() {
    await this.page.goto('/cart');
  }

  async getCartItem(productId: string): Promise<Locator> {
    return this.page.locator(`[data-testid="cart-item-${productId}"]`);
  }

  async increaseQuantity(productId: string) {
    const item = await this.getCartItem(productId);
    await item.getByTestId('cart-item-increase').click();
  }

  async decreaseQuantity(productId: string) {
    const item = await this.getCartItem(productId);
    await item.getByTestId('cart-item-decrease').click();
  }

  async removeItem(productId: string) {
    const item = await this.getCartItem(productId);
    await item.getByTestId('cart-item-remove').click();
  }

  async getQuantity(productId: string): Promise<string> {
    const item = await this.getCartItem(productId);
    return item.getByTestId('cart-item-quantity').innerText();
  }

  async proceedToCheckout() {
    await this.checkoutBtn.click();
  }

  async isEmpty(): Promise<boolean> {
    return this.emptyCartMessage.isVisible();
  }
}
