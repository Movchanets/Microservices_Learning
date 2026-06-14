import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/seller/products` — seller product list.
 */
export class SellerProductsPage extends BasePage {
  readonly addProductBtn: Locator;
  readonly productsList: Locator;
  readonly productRows: Locator;
  readonly emptyState: Locator;
  readonly loadingSpinner: Locator;
  readonly searchInput: Locator;

  constructor(page: Page) {
    super(page);
    // "Add Product" is an <a> link, not a button
    this.addProductBtn = page.getByRole('link', { name: /add product/i });
    // Product list is <ul> with <li> items, not a <table>
    this.productsList = page.locator('ul.divide-y');
    this.productRows = this.productsList.locator('> li');
    this.emptyState = page.getByText(/no products yet/i);
    this.loadingSpinner = page.locator('[class*="animate-spin"]');
    this.searchInput = page.getByPlaceholder(/search/i);
  }

  get url(): string {
    return '/seller/products';
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

  /** Click the edit link for a product row (navigates to /seller/products/:id/edit). */
  async editProduct(index: number) {
    const row = this.productRows.nth(index);
    // Edit is an <a> with Pencil icon — match by href pattern
    await row.locator('a[href*="/seller/products/"][href$="/edit"]').click();
  }

  /** Click the delete button for a product row (triggers browser confirm dialog). */
  async deleteProduct(index: number) {
    const row = this.productRows.nth(index);
    // Delete is the last <button> in the actions area (Trash2 icon)
    await row.locator('button').last().click();
  }

  /** Click the activate button for a product row. */
  async activateProduct(index: number) {
    const row = this.productRows.nth(index);
    await row.getByTestId('product-activate').click();
  }

  /** Click the deactivate button for a product row. */
  async deactivateProduct(index: number) {
    const row = this.productRows.nth(index);
    await row.getByTestId('product-deactivate').click();
  }

  async getProductName(index: number): Promise<string> {
    const row = this.productRows.nth(index);
    return row.locator('p.font-medium').innerText();
  }

  async getProductStatus(index: number): Promise<string> {
    const row = this.productRows.nth(index);
    return row.locator('span.text-xs').innerText();
  }

  /** Returns combined SKU count + price text (e.g. "2 SKUs · $29.99"). */
  async getProductMeta(index: number): Promise<string> {
    const row = this.productRows.nth(index);
    return row.locator('p.text-sm.text-muted').innerText();
  }

  async isLoading(): Promise<boolean> {
    return this.loadingSpinner.isVisible();
  }

  async isEmpty(): Promise<boolean> {
    return this.emptyState.isVisible();
  }
}
