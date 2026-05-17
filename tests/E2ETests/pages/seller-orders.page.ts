import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class SellerOrdersPage extends BasePage {
  readonly pageHeading: Locator;
  readonly ordersTable: Locator;
  readonly orderRows: Locator;
  readonly emptyMessage: Locator;
  readonly loadingSpinner: Locator;

  // Status update
  readonly updateStatusBtns: Locator;
  readonly statusSelects: Locator;
  readonly notesInputs: Locator;
  readonly confirmUpdateBtns: Locator;
  readonly cancelUpdateBtns: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole('heading', { name: 'Orders' });
    this.ordersTable = page.locator('table');
    this.orderRows = this.ordersTable.locator('tbody tr');
    this.emptyMessage = page.getByText('No orders yet');
    this.loadingSpinner = page.locator('[class*="animate-spin"]');

    this.updateStatusBtns = page.getByRole('button', { name: /update status|mark as/i });
    this.statusSelects = page.locator('select');
    this.notesInputs = page.locator('textarea, input[placeholder*="note"]');
    this.confirmUpdateBtns = page.getByRole('button', { name: /confirm|update/i });
    this.cancelUpdateBtns = page.getByRole('button', { name: /cancel/i });
  }

  async goto() {
    await this.page.goto('/seller/dashboard');
    const ordersTab = this.page.getByRole('link', { name: /orders/i });
    if (await ordersTab.isVisible()) {
      await ordersTab.click();
    }
  }

  async getOrderCount(): Promise<number> {
    return this.orderRows.count();
  }

  async getRowByOrderId(orderId: string): Promise<Locator> {
    return this.orderRows.filter({ hasText: orderId });
  }

  async getStatusForOrder(orderId: string): Promise<string> {
    const row = await this.getRowByOrderId(orderId);
    const statusBadge = row.locator('span[class*="rounded-full"]');
    return statusBadge.innerText();
  }

  async clickUpdateStatus(orderId: string) {
    const row = await this.getRowByOrderId(orderId);
    const updateBtn = row.getByRole('button', { name: /update|mark as|next/i });
    await updateBtn.click();
  }

  async confirmStatusUpdate(orderId: string, notes?: string) {
    if (notes) {
      const notesInput = this.page.locator('textarea, input[placeholder*="note"]').last();
      await notesInput.fill(notes);
    }
    const confirmBtn = this.page.getByRole('button', { name: /confirm|update/i }).last();
    await confirmBtn.click();
  }

  async isLoading(): Promise<boolean> {
    return this.loadingSpinner.isVisible();
  }

  async isEmpty(): Promise<boolean> {
    return this.emptyMessage.isVisible();
  }

  async getOrderIds(): Promise<string[]> {
    const rows = await this.orderRows.all();
    const ids: string[] = [];
    for (const row of rows) {
      const idCell = row.locator('td').first();
      const text = await idCell.innerText();
      ids.push(text.trim());
    }
    return ids;
  }
}
