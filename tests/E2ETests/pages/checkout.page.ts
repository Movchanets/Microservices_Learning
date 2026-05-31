import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/checkout` — order checkout flow.
 */
export class CheckoutPage extends BasePage {
  readonly pageHeading: Locator;
  readonly confirmOrderBtn: Locator;
  readonly backToCartLink: Locator;
  readonly emptyCartMessage: Locator;
  
  // Submission elements
  readonly orderSubmittedHeading: Locator;
  readonly correlationIdText: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole('heading', { name: 'Checkout' });
    this.confirmOrderBtn = page.getByRole('button', { name: 'Confirm Order' });
    this.backToCartLink = page.getByRole('link', { name: 'Back to Cart' });
    this.emptyCartMessage = page.getByText('Your cart is empty');
    
    this.orderSubmittedHeading = page.getByRole('heading', { name: 'Order Submitted' });
    this.correlationIdText = page.locator('p.font-mono');
  }

  async goto() {
    await this.page.goto('/checkout');
  }

  async confirmOrder() {
    await this.confirmOrderBtn.click();
  }

  async getCorrelationId(): Promise<string> {
    return await this.correlationIdText.innerText();
  }
}
