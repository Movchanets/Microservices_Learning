import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class SellerProductsPage extends BasePage {
  readonly addProductBtn: Locator;
  readonly productsTable: Locator;
  readonly productRows: Locator;
  readonly emptyState: Locator;
  readonly loadingSpinner: Locator;
  readonly searchInput: Locator;
  readonly editBtns: Locator;
  readonly deleteBtns: Locator;
  readonly confirmDeleteBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.addProductBtn = page.getByRole('button', { name: /add product|create product|new product/i });
    this.productsTable = page.locator('table');
    this.productRows = this.productsTable.locator('tbody tr');
    this.emptyState = page.getByText(/no products|empty/i);
    this.loadingSpinner = page.locator('[class*="animate-spin"]');
    this.searchInput = page.getByPlaceholder(/search/i);
    this.editBtns = page.getByRole('button', { name: /edit/i });
    this.deleteBtns = page.getByRole('button', { name: /delete|remove/i });
    this.confirmDeleteBtn = page.getByRole('button', { name: /confirm|yes.*delete/i });
  }

  async goto() {
    await super.goto('/seller/products');
    await this.page.waitForLoadState('domcontentloaded');
  }

  async getProductCount(): Promise<number> {
    return this.productRows.count();
  }

  async getProductRow(index: number) {
    return this.productRows.nth(index);
  }

  async clickAddProduct() {
    await this.addProductBtn.click();
  }

  async editProduct(index: number) {
    const row = this.productRows.nth(index);
    await row.getByRole('button', { name: /edit/i }).click();
  }

  async deleteProduct(index: number) {
    const row = this.productRows.nth(index);
    await row.getByRole('button', { name: /delete|remove/i }).click();
  }

  async confirmDelete() {
    await this.confirmDeleteBtn.click();
  }

  async searchProducts(query: string) {
    await this.searchInput.fill(query);
  }

  async getProductName(index: number): Promise<string> {
    const row = this.productRows.nth(index);
    const nameCell = row.locator('td').first();
    return nameCell.innerText();
  }

  async getProductStatus(index: number): Promise<string> {
    const row = this.productRows.nth(index);
    const statusCell = row.locator('td').nth(2);
    return statusCell.innerText();
  }

  /**
   * Returns the price range text for a product row.
   * With the new SKU model, products show minPrice–maxPrice.
   */
  async getProductPriceRange(index: number): Promise<string> {
    const row = this.productRows.nth(index);
    // Price is typically the 3rd or 4th column
    const priceCell = row.locator('td').nth(3);
    return priceCell.innerText();
  }

  /**
   * Returns the SKU count text for a product row.
   */
  async getProductSkuCount(index: number): Promise<string> {
    const row = this.productRows.nth(index);
    const skuCountCell = row.locator('td').nth(4);
    return skuCountCell.innerText();
  }

  async isLoading(): Promise<boolean> {
    return this.loadingSpinner.isVisible();
  }

  async isEmpty(): Promise<boolean> {
    return this.emptyState.isVisible();
  }
}
