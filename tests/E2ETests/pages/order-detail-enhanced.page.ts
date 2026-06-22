import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/orders/:id` — enhanced order detail with timeline.
 */
export class OrderDetailEnhancedPage extends BasePage {
  readonly pageHeading: Locator;
  readonly backToOrdersLink: Locator;
  readonly orderIdText: Locator;
  readonly statusBadge: Locator;
  readonly totalAmountText: Locator;
  readonly createdAtText: Locator;
  readonly completedAtText: Locator;
  readonly orderItemsList: Locator;
  readonly orderItems: Locator;

  // Cancel order
  readonly cancelOrderBtn: Locator;
  readonly cancelConfirmDialog: Locator;
  readonly cancelReasonInput: Locator;
  readonly confirmCancelBtn: Locator;
  readonly cancelDialogCancelBtn: Locator;

  // Timeline
  readonly timeline: Locator;
  readonly timelineSteps: Locator;

  // Loading & error
  readonly loadingSpinner: Locator;
  readonly errorMessage: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole('heading', { name: 'Order Details' });
    this.backToOrdersLink = page.getByRole('link', { name: 'Back to Orders' });
    this.orderIdText = page.locator('h1 + p.font-mono, h2 + p.font-mono');
    this.statusBadge = page.locator('app-status-badge');
    this.totalAmountText = page.getByTestId('order-total');
    this.createdAtText = page.getByTestId('order-created-at');
    this.completedAtText = page.getByTestId('order-completed-at');
    this.orderItemsList = page.locator('ul.divide-y');
    this.orderItems = this.orderItemsList.locator('li');

    // Cancel order
    this.cancelOrderBtn = page.getByRole('button', { name: /cancel order/i });
    this.cancelConfirmDialog = page.locator('[class*="fixed"], [class*="absolute"], dialog').filter({ hasText: /cancel.*order|are you sure/i });
    this.cancelReasonInput = this.cancelConfirmDialog.locator('input, textarea');
    this.confirmCancelBtn = this.cancelConfirmDialog.getByRole('button', { name: /confirm|cancel order/i });
    this.cancelDialogCancelBtn = this.cancelConfirmDialog.getByRole('button', { name: /close|dismiss|no/i });

    // Timeline
    this.timeline = page.locator('app-order-timeline');
    this.timelineSteps = this.timeline.locator('[class*="flex"][class*="items-start"], [class*="step"]');

    // Loading & error
    this.loadingSpinner = page.locator('[class*="animate-spin"]');
    this.errorMessage = page.locator('[class*="text-red"], [role="alert"]');
  }

  get url(): string {
    return '/orders';
  }


  async goto(orderId: string) {
    await this.page.goto(`/orders/${orderId}`);
  }

  async waitForLoaded() {
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: TIMEOUTS.element });
  }

  async getOrderId(): Promise<string> {
    return this.orderIdText.innerText();
  }

  async getStatus(): Promise<string> {
    return this.statusBadge.innerText();
  }

  async getTotalAmount(): Promise<string> {
    return this.totalAmountText.innerText();
  }

  async getOrderItem(sku: string): Promise<Locator> {
    return this.orderItems.filter({ hasText: sku });
  }

  async getOrderItemCount(): Promise<number> {
    return this.orderItems.count();
  }

  async hasCancelButton(): Promise<boolean> {
    return this.cancelOrderBtn.isVisible();
  }

  async clickCancelOrder() {
    await this.cancelOrderBtn.click();
  }

  async confirmCancel(reason?: string) {
    if (reason) {
      await this.cancelReasonInput.fill(reason);
    }
    await this.confirmCancelBtn.click();
  }

  async dismissCancel() {
    await this.cancelDialogCancelBtn.click();
  }

  async getTimelineStepCount(): Promise<number> {
    return this.timelineSteps.count();
  }

  async isLoading(): Promise<boolean> {
    return this.loadingSpinner.isVisible();
  }

  async hasError(): Promise<boolean> {
    return this.errorMessage.isVisible();
  }

  async getErrorMessage(): Promise<string> {
    return this.errorMessage.innerText();
  }
}
