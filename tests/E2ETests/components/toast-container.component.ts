import { Locator, Page } from '@playwright/test';

export class ToastContainerComponent {
  readonly page: Page;
  readonly container: Locator;
  readonly toasts: Locator;

  constructor(page: Page) {
    this.page = page;
    this.container = page.locator('app-toast-container');
    this.toasts = this.container.locator('[class*="rounded-xl"][class*="shadow-lg"]');
  }

  async waitForToast(type?: 'success' | 'error' | 'info', timeout = 10000): Promise<void> {
    const toast = type
      ? this.toasts.filter({ hasText: new RegExp(type, 'i') }).first()
      : this.toasts.first();
    await expect(toast).toBeVisible({ timeout });
  }

  async getToastMessage(index = 0): Promise<string> {
    const toast = this.toasts.nth(index);
    return toast.locator('span').first().innerText();
  }

  async getToastCount(): Promise<number> {
    return this.toasts.count();
  }

  async dismissToast(index = 0) {
    const dismissBtn = this.toasts.nth(index).locator('button');
    await dismissBtn.click();
  }

  async expectSuccessToast(message: string, timeout = 10000): Promise<void> {
    const toast = this.toasts.filter({ hasText: message });
    await expect(toast).toBeVisible({ timeout });
  }

  async expectErrorToast(message: string, timeout = 10000): Promise<void> {
    const toast = this.toasts.filter({ hasText: message });
    await expect(toast).toBeVisible({ timeout });
  }

  async isVisible(): Promise<boolean> {
    return this.container.isVisible();
  }
}
