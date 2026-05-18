import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

export class StoreSettingsPage extends BasePage {
  readonly storeNameInput: Locator;
  readonly storeDescInput: Locator;
  readonly contactEmailInput: Locator;
  readonly saveBtn: Locator;
  readonly createStoreBtn: Locator;
  readonly storeNameDisplay: Locator;
  readonly storeStatus: Locator;

  constructor(page: Page) {
    super(page);
    this.storeNameInput = page.getByTestId('store-name-input');
    this.storeDescInput = page.locator('textarea');
    this.contactEmailInput = page.locator('input[type="email"]');
    this.saveBtn = page.getByRole('button', { name: /save changes/i });
    this.createStoreBtn = page.getByRole('button', { name: /create store/i });
    this.storeNameDisplay = page.getByTestId('store-name');
    this.storeStatus = page.getByTestId('store-status');
  }

  async goto() {
    await this.page.goto('/seller/settings');
  }

  async createStore(name: string, description: string) {
    await this.storeNameInput.fill(name);
    await this.storeDescInput.fill(description);
    await this.createStoreBtn.click();
  }

  async updateStore(name: string, description: string) {
    await this.storeNameInput.fill(name);
    await this.storeDescInput.fill(description);
    await this.saveBtn.click();
  }
}
