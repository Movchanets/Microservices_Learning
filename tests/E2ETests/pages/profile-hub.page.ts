import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class ProfileHubPage extends BasePage {
  readonly pageHeading: Locator;
  readonly sidebar: Locator;
  readonly ordersTab: Locator;
  readonly settingsTab: Locator;
  readonly activeTab: Locator;

  // Profile info
  readonly userName: Locator;
  readonly userEmail: Locator;
  readonly editProfileBtn: Locator;

  // Orders tab
  readonly ordersList: Locator;
  readonly orderItems: Locator;
  readonly emptyOrdersMessage: Locator;

  // Settings tab
  readonly firstNameInput: Locator;
  readonly lastNameInput: Locator;
  readonly emailInput: Locator;
  readonly saveProfileBtn: Locator;
  readonly changePasswordBtn: Locator;
  readonly currentPasswordInput: Locator;
  readonly newPasswordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly updatePasswordBtn: Locator;

  // Feedback
  readonly successMessage: Locator;
  readonly errorMessage: Locator;
  readonly validationErrors: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole('heading', { name: /my account|profile|my orders/i });
    this.sidebar = page.locator('aside, nav').filter({ hasText: /orders|settings|messages/i });
    this.ordersTab = page.getByRole('link', { name: /orders/i }).or(page.getByRole('button', { name: /orders/i }));
    this.settingsTab = page.getByRole('link', { name: /settings/i }).or(page.getByRole('button', { name: /settings/i }));
    this.activeTab = page.locator('[aria-current="page"], .active, .bg-primary');

    this.userName = page.locator('h1, h2').filter({ hasText: /\w+/ }).first();
    this.userEmail = page.locator('p, span').filter({ hasText: /@/ }).first();
    this.editProfileBtn = page.getByRole('button', { name: /edit profile/i });

    this.ordersList = page.locator('ul, table').first();
    this.orderItems = this.ordersList.locator('li, tr').filter({ has: page.locator('a') });
    this.emptyOrdersMessage = page.getByText(/no orders|no recent orders/i);

    this.firstNameInput = page.locator('input[formcontrolname="firstName"]');
    this.lastNameInput = page.locator('input[formcontrolname="lastName"]');
    this.emailInput = page.getByLabel(/email/i).or(page.getByPlaceholder(/email/i));
    this.saveProfileBtn = page.getByRole('button', { name: /save|update profile/i });
    this.changePasswordBtn = page.getByRole('button', { name: /change password/i });
    this.currentPasswordInput = page.locator('input[formcontrolname="currentPassword"]');
    this.newPasswordInput = page.locator('input[formcontrolname="newPassword"]');
    this.confirmPasswordInput = page.locator('input[formcontrolname="confirmPassword"]');
    this.updatePasswordBtn = page.getByRole('button', { name: /update password/i });

    // Feedback messages
    this.successMessage = page.getByText(/success|updated|saved/i).or(page.locator('.text-green'));
    this.errorMessage = page.getByText(/error|failed|incorrect/i).or(page.locator('[role="alert"], .text-red'));
    this.validationErrors = page.locator('.text-red-500, [aria-live="polite"]');
  }

  async goto() {
    await this.page.goto('/profile');
    await this.waitForPageLoad();
  }

  async navigateToOrders() {
    await this.ordersTab.click();
  }

  async navigateToSettings() {
    await this.settingsTab.click();
  }

  async getOrderCount(): Promise<number> {
    return this.orderItems.count();
  }

  async viewOrder(index: number) {
    await this.orderItems.nth(index).locator('a').first().click();
  }

  async updateProfile(firstName: string, lastName: string) {
    await this.firstNameInput.fill(firstName);
    await this.lastNameInput.fill(lastName);
    await this.saveProfileBtn.click();
  }

  async changePassword(current: string, newPass: string, confirm: string) {
    await this.changePasswordBtn.click();
    await this.currentPasswordInput.fill(current);
    await this.newPasswordInput.fill(newPass);
    await this.confirmPasswordInput.fill(confirm);
    await this.updatePasswordBtn.click();
  }

  async expectProfileUpdateSuccess(timeout = 10000) {
    await expect(this.successMessage).toBeVisible({ timeout });
  }

  async expectPasswordChangeSuccess(timeout = 10000) {
    await expect(this.successMessage).toBeVisible({ timeout });
  }

  async expectValidationError(message: string, timeout = 5000) {
    await expect(this.validationErrors.filter({ hasText: message })).toBeVisible({ timeout });
  }

  async expectError(message?: string, timeout = 10000) {
    if (message) {
      await expect(this.errorMessage.filter({ hasText: message })).toBeVisible({ timeout });
    } else {
      await expect(this.errorMessage).toBeVisible({ timeout });
    }
  }
}
