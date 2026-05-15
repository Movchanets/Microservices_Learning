import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

export class CartPage extends BasePage {
  readonly pageHeading: Locator;
  readonly emptyCartMessage: Locator;
  readonly continueShoppingBtn: Locator;
  readonly cartItemsList: Locator;
  readonly cartItems: Locator;
  readonly totalItemsCount: Locator;
  readonly checkoutBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole('heading', { name: 'Your Cart' });
    this.emptyCartMessage = page.getByText('Your cart is empty');
    this.continueShoppingBtn = page.getByRole('link', { name: 'Continue Shopping' });
    
    this.cartItemsList = page.locator('ul.divide-y');
    this.cartItems = this.cartItemsList.locator('li');
    
    // Finds the total items count which is usually a large text next to 'Total Items'
    this.totalItemsCount = page.locator('p.text-2xl.font-bold');
    this.checkoutBtn = page.getByRole('button', { name: 'Checkout' });
  }

  async goto() {
    await this.page.goto('/cart');
  }

  async getCartItem(sku: string): Promise<Locator> {
    return this.cartItems.filter({ hasText: sku });
  }

  async increaseQuantity(sku: string) {
    const item = await this.getCartItem(sku);
    // The second button in the item's div is typically the plus button, 
    // but better to use the lucide-icon or button properties if available.
    // However, given the structure, plus is the second button in the quantity control.
    // We can rely on aria-label or specific classes if added, but here we can find the button containing the Plus icon.
    await item.locator('button').filter({ has: this.page.locator('lucide-icon[name="Plus"]') }).click();
  }

  async decreaseQuantity(sku: string) {
    const item = await this.getCartItem(sku);
    await item.locator('button').filter({ has: this.page.locator('lucide-icon[name="Minus"]') }).click();
  }

  async removeItem(sku: string) {
    const item = await this.getCartItem(sku);
    await item.locator('button').filter({ has: this.page.locator('lucide-icon[name="Trash2"]') }).click();
  }

  async proceedToCheckout() {
    await this.checkoutBtn.click();
  }
}
