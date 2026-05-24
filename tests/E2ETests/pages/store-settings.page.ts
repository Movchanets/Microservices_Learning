import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class StoreSettingsPage extends BasePage {
  readonly storeNameInput: Locator;
  readonly storeDescInput: Locator;
  readonly contactEmailInput: Locator;
  readonly saveBtn: Locator;
  readonly createStoreBtn: Locator;
  readonly storeNameDisplay: Locator;
  readonly storeStatus: Locator;
  readonly successMessage: Locator;
  readonly errorMessage: Locator;
  readonly loadingSpinner: Locator;

  constructor(page: Page) {
    super(page);
    this.storeNameInput = page.getByTestId('store-name-input');
    this.storeDescInput = page.locator('textarea');
    this.contactEmailInput = page.locator('input[type="email"]');
    this.saveBtn = page.getByRole('button', { name: /save changes/i });
    this.createStoreBtn = page.getByRole('button', { name: /create store/i });
    this.storeNameDisplay = page.getByTestId('store-name');
    this.storeStatus = page.getByTestId('store-status');
    this.successMessage = page.getByText(/success|saved|updated/i).or(page.locator('.text-green'));
    this.errorMessage = page.getByText(/error|failed/i).or(page.locator('[role="alert"]'));
    this.loadingSpinner = page.locator('[class*="animate-spin"]');
  }

  async goto() {
    await this.page.goto('/seller/settings');
    await this.waitForPageLoad();
  }

  async createStore(name: string, description: string) {
    await this.fillStable(this.storeNameInput, name);
    await this.storeDescInput.fill(description);
    await this.createStoreBtn.click();
  }

  async updateStore(name: string, description: string) {
    await this.fillStable(this.storeNameInput, name);
    await this.storeDescInput.fill(description);
    await this.saveBtn.click();
  }

  async fillStoreForm(name: string, description: string, email?: string) {
    await this.fillStable(this.storeNameInput, name);
    await this.storeDescInput.fill(description);
    if (email) {
      await this.fillStable(this.contactEmailInput, email);
    }
  }

  async save() {
    await this.saveBtn.click();
  }

  async expectSuccess(timeout = 10000) {
    await expect(this.successMessage).toBeVisible({ timeout });
  }

  async expectError(message?: string, timeout = 10000) {
    if (message) {
      await expect(this.errorMessage.filter({ hasText: message })).toBeVisible({ timeout });
    } else {
      await expect(this.errorMessage).toBeVisible({ timeout });
    }
  }

  async isLoading(): Promise<boolean> {
    return this.loadingSpinner.isVisible();
  }
}
