import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/orders` — order history list.
 */
export class OrdersPage extends BasePage {
  readonly pageHeading: Locator;
  readonly startShoppingBtn: Locator;
  readonly activeOrdersSection: Locator;
  readonly completedOrdersSection: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole('heading', { name: 'My Orders' });
    this.startShoppingBtn = page.getByRole('link', { name: 'Start Shopping' });
    
    // We can locate sections by their headings
    this.activeOrdersSection = page.locator('div').filter({ has: page.getByRole('heading', { name: 'Active Orders' }) });
    this.completedOrdersSection = page.locator('div').filter({ has: page.getByRole('heading', { name: 'Completed Orders' }) });
  }

  get url(): string {
    return '/orders';
  }

  async viewOrderDetails(orderIdPart: string) {
    // orderIdPart can be a truncated ID that shows in the list
    await this.page.getByRole('link').filter({ hasText: orderIdPart }).click();
  }
}
