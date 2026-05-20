import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

export class SellerProductsPage extends BasePage {
  readonly addProductBtn: Locator;
  readonly productsTable: Locator;
  readonly productRows: Locator;
  readonly emptyState: Locator;
  readonly loadingSpinner: Locator;

  constructor(page: Page) {
    super(page);
    this.addProductBtn = page.getByRole('button', { name: /add product|create product|new product/i });
    this.productsTable = page.locator('table');
    this.productRows = this.productsTable.locator('tbody tr');
    this.emptyState = page.getByText(/no products|empty/i);
    this.loadingSpinner = page.locator('[class*="animate-spin"]');
  }

  async goto() {
    await super.goto('/seller/products');
    await this.page.waitForLoadState('domcontentloaded');
  }

  async getProductCount(): Promise<number> {
    return await this.productRows.count();
  }

  async getProductRow(index: number) {
    return this.productRows.nth(index);
  }
}
