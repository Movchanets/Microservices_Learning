import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

export class SellerDashboardPage extends BasePage {
  readonly pageHeading: Locator;
  readonly settingsIcon: Locator;
  readonly productsTab: Locator;
  readonly settingsTab: Locator;
  readonly salesCard: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole('heading', { name: 'Seller Dashboard' });
    this.settingsIcon = page.locator('a[routerLink="/seller/settings"]').first();
    this.productsTab = page.getByRole('link', { name: 'Products' });
    this.settingsTab = page.getByRole('link', { name: 'Settings' });
    this.salesCard = page.locator('app-sales-card');
  }

  async goto() {
    await this.page.goto('/seller/dashboard');
  }

  async navigateToProducts() {
    await this.productsTab.click();
  }

  async navigateToSettings() {
    await this.settingsTab.click();
  }
}
