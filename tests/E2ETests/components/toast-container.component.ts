import { Locator, Page } from '@playwright/test';
import { BaseComponent } from './base.component';

/**
 * Component object for the toast notification container.
 *
 * Scoped to `<app-toast-container>`. Toasts are transient — use `waitForToast()`
 * or `waitForSuccessToast()` before asserting on content.
 */
export class ToastContainerComponent extends BaseComponent {
  readonly toasts: Locator;

  constructor(page: Page) {
    const root = page.locator('app-toast-container');
    super(page, root);
    this.toasts = this.root.locator('[class*="rounded-xl"][class*="shadow-lg"]');
  }

  async waitForToast(type?: 'success' | 'error' | 'info', timeout = 10000): Promise<void> {
    const toast = type
      ? this.toasts.filter({ hasText: new RegExp(type, 'i') }).first()
      : this.toasts.first();
    await toast.waitFor({ state: 'visible', timeout });
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

  async waitForSuccessToast(message: string, timeout = 10000): Promise<void> {
    const toast = this.toasts.filter({ hasText: message });
    await toast.waitFor({ state: 'visible', timeout });
  }

  async waitForErrorToast(message: string, timeout = 10000): Promise<void> {
    const toast = this.toasts.filter({ hasText: message });
    await toast.waitFor({ state: 'visible', timeout });
  }
}
