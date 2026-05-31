import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/orders/:id` — single order detail view.
 */
export class OrderDetailPage extends BasePage {
  readonly pageHeading: Locator;
  readonly backToOrdersLink: Locator;
  readonly orderIdText: Locator;
  readonly statusBadge: Locator;
  readonly totalAmountText: Locator;
  readonly orderItemsList: Locator;
  readonly orderItems: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole('heading', { name: 'Order Details' });
    this.backToOrdersLink = page.getByRole('link', { name: 'Back to Orders' });
    
    // ID is usually below the heading
    this.orderIdText = page.locator('h1 + p.font-mono');
    this.statusBadge = page.locator('app-status-badge');
    
    // Total amount in the grid
    this.totalAmountText = page.getByTestId('order-total');
    
    this.orderItemsList = page.locator('ul.divide-y');
    this.orderItems = this.orderItemsList.locator('li');
  }

  async getOrderId(): Promise<string> {
    return await this.orderIdText.innerText();
  }

  async getStatus(): Promise<string> {
    return await this.statusBadge.innerText();
  }

  async getTotalAmount(): Promise<string> {
    return await this.totalAmountText.innerText();
  }

  async getOrderItem(sku: string): Promise<Locator> {
    return this.orderItems.filter({ hasText: sku });
  }
}
