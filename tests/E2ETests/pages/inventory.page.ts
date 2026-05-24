import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class InventoryPage extends BasePage {
  readonly pageHeading: Locator;
  readonly lowStockAlert: Locator;
  readonly viewLowStockBtn: Locator;

  // Filters
  readonly allItemsFilter: Locator;
  readonly lowStockFilter: Locator;
  readonly outOfStockFilter: Locator;

  // Inventory table
  readonly inventoryTable: Locator;
  readonly inventoryRows: Locator;
  readonly emptyMessage: Locator;
  readonly loadingSkeleton: Locator;

  // Add stock
  readonly addStockInputs: Locator;
  readonly addStockBtns: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole('heading', { name: /inventory/i });
    this.lowStockAlert = page.getByTestId('low-stock-alert');
    this.viewLowStockBtn = this.lowStockAlert.getByRole('button', { name: /view items/i });

    this.allItemsFilter = page.getByRole('button', { name: 'All Items' });
    this.lowStockFilter = page.getByRole('button', { name: 'Low Stock' });
    this.outOfStockFilter = page.getByRole('button', { name: 'Out of Stock' });

    this.inventoryTable = page.locator('table');
    this.inventoryRows = this.inventoryTable.locator('tbody tr');
    this.emptyMessage = page.getByText(/no inventory items/i);
    this.loadingSkeleton = page.locator('[class*="animate-pulse"]');

    this.addStockInputs = page.locator('input[type="number"]');
    this.addStockBtns = page.getByRole('button', { name: /add stock/i });
  }

  async goto() {
    await this.page.goto('/seller/dashboard');
    // Navigate to inventory tab if needed
    const inventoryTab = this.page.getByRole('link', { name: /inventory/i });
    if (await inventoryTab.isVisible()) {
      await inventoryTab.click();
    }
  }

  async filterAll() {
    await this.allItemsFilter.click();
  }

  async filterLowStock() {
    await this.lowStockFilter.click();
  }

  async filterOutOfStock() {
    await this.outOfStockFilter.click();
  }

  async getRowCount(): Promise<number> {
    return this.inventoryRows.count();
  }

  async getRowBySku(sku: string): Promise<Locator> {
    return this.inventoryRows.filter({ hasText: sku });
  }

  async getStatusForRow(sku: string): Promise<string> {
    const row = await this.getRowBySku(sku);
    const statusCell = row.locator('td').nth(3); // Status is typically 4th column
    return statusCell.innerText();
  }

  async getQuantityForRow(sku: string): Promise<string> {
    const row = await this.getRowBySku(sku);
    const qtyCell = row.locator('td').nth(2); // Quantity is typically 3rd column
    return qtyCell.innerText();
  }

  async addStock(sku: string, quantity: number) {
    const row = await this.getRowBySku(sku);
    const input = row.locator('input[type="number"]');
    const btn = row.getByRole('button', { name: /add/i });
    await input.fill(String(quantity));
    await btn.click();
  }

  async isLoading(): Promise<boolean> {
    return this.loadingSkeleton.isVisible();
  }

  async isEmpty(): Promise<boolean> {
    return this.emptyMessage.isVisible();
  }

  async hasLowStockAlert(): Promise<boolean> {
    return this.lowStockAlert.isVisible();
  }

  async getLowStockCount(): Promise<string> {
    const text = await this.lowStockAlert.innerText();
    const match = text.match(/(\d+)/);
    return match ? match[1] : '0';
  }
}
