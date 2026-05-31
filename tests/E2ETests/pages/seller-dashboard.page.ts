import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';
import { TIMEOUTS } from '../utils/constants';

/**
 * Page object for `/seller` — seller dashboard overview.
 * Handles the "Create Your Store" screen when seller has no store yet.
 */
export class SellerDashboardPage extends BasePage {
  readonly pageHeading: Locator;
  readonly productsTab: Locator;
  readonly ordersLink: Locator;
  readonly settingsTab: Locator;
  readonly salesCard: Locator;

  // Create Store form
  readonly createStoreHeading: Locator;
  readonly storeNameInput: Locator;
  readonly storeDescInput: Locator;
  readonly createStoreBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole('heading', { name: 'Seller Dashboard' });
    this.productsTab = page.getByRole('link', { name: 'Products' });
    this.settingsTab = page.getByRole('link', { name: 'Settings' });
    this.ordersLink = page.getByRole('link', { name: 'Orders' });
    this.salesCard = page.locator('app-sales-card');

    this.createStoreHeading = page.getByRole('heading', { name: 'Create Your Store' });
    this.storeNameInput = page.getByTestId('store-name-input');
    this.storeDescInput = page.getByPlaceholder('Tell customers what your store is about...');
    this.createStoreBtn = page.getByRole('button', { name: 'Create Store' });
  }

  async goto() {
    await this.page.goto('/seller');
  }

  /** Fill the Create Your Store form and submit. */
  async createStore(name: string, description: string): Promise<void> {
    await expect(this.storeNameInput).toBeVisible({ timeout: TIMEOUTS.element });
    await this.storeNameInput.fill(name);
    await this.storeNameInput.press('Tab');
    await this.storeDescInput.fill(description);
    await this.storeDescInput.press('Tab');
    await expect(this.createStoreBtn).toBeEnabled({ timeout: TIMEOUTS.element });
    await this.createStoreBtn.click();
    await this.productsTab.waitFor({ state: 'visible', timeout: TIMEOUTS.api });
  }
}
