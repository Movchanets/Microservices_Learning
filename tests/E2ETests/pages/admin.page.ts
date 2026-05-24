import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class AdminPage extends BasePage {
  readonly pageHeading: Locator;
  readonly usersTab: Locator;
  readonly verificationsTab: Locator;
  readonly allStoresTab: Locator;

  // Stats
  readonly statsCards: Locator;

  // Users List
  readonly usersTable: Locator;
  readonly userRows: Locator;
  readonly searchInput: Locator;
  readonly noUsersMessage: Locator;
  readonly deactivateConfirmDialog: Locator;

  // Verifications List
  readonly verificationCards: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole('heading', { name: 'Admin Panel' });
    this.usersTab = page.getByRole('link', { name: 'Users' });
    this.verificationsTab = page.getByRole('link', { name: 'Verifications' });
    this.allStoresTab = page.getByRole('link', { name: 'All Stores' });

    this.statsCards = page.locator('app-stats-card');
    this.usersTable = page.getByRole('table');
    this.userRows = this.usersTable.locator('tbody tr');
    this.searchInput = page.getByPlaceholder(/search/i);
    this.noUsersMessage = page.getByText(/no users found/i);
    this.deactivateConfirmDialog = page.locator('[role="dialog"], .fixed').filter({ hasText: /deactivat|are you sure/i });
    this.verificationCards = page.getByTestId('store-verification-card');
  }

  async goto() {
    await this.page.goto('/admin');
    await this.waitForPageLoad();
  }

  async navigateToUsers() {
    await this.usersTab.click();
  }

  async navigateToVerifications() {
    await this.verificationsTab.click();
  }

  async navigateToAllStores() {
    await this.allStoresTab.click();
  }

  async getStatValue(label: string): Promise<string> {
    const card = this.statsCards.filter({ hasText: label });
    return await card.locator('p.text-2xl').innerText();
  }

  async searchUsers(query: string) {
    await this.searchInput.fill(query);
  }

  async getUserCount(): Promise<number> {
    return this.userRows.count();
  }

  // Users Actions
  async getUserRow(name: string): Promise<Locator> {
    return this.userRows.filter({ hasText: name });
  }

  async changeUserRole(name: string, newRole: 'Buyer' | 'Seller' | 'Admin') {
    const row = await this.getUserRow(name);
    const select = row.locator('select');
    await select.selectOption(newRole);
  }

  async deactivateUser(name: string) {
    const row = await this.getUserRow(name);
    const deactivateBtn = row.getByRole('button', { name: /deactivate|userx/i });

    // Register dialog handler BEFORE clicking
    const dialogPromise = this.page.waitForEvent('dialog', { timeout: 5000 }).catch(() => null);
    await deactivateBtn.click();
    const dialog = await dialogPromise;
    if (dialog) {
      await dialog.accept();
    }
  }

  async confirmDeactivation() {
    // For apps that use a custom modal instead of browser dialog
    const confirmBtn = this.deactivateConfirmDialog.getByRole('button', { name: /confirm|yes/i });
    await confirmBtn.click();
  }

  // Verification Actions
  async getVerificationCard(storeName: string): Promise<Locator> {
    return this.verificationCards.filter({ hasText: storeName });
  }

  async approveStore(storeName: string) {
    const card = await this.getVerificationCard(storeName);
    const approveBtn = card.getByRole('button', { name: 'Approve' });
    await approveBtn.click();
  }

  async rejectStore(storeName: string, reason: string) {
    const card = await this.getVerificationCard(storeName);
    const rejectBtn = card.getByRole('button', { name: 'Reject' });

    const dialogPromise = this.page.waitForEvent('dialog', { timeout: 5000 }).catch(() => null);
    await rejectBtn.click();
    const dialog = await dialogPromise;
    if (dialog) {
      await dialog.accept(reason);
    }
  }

  async viewStoreDetails(storeName: string) {
    const card = await this.getVerificationCard(storeName);
    const detailsLink = card.getByRole('link', { name: 'Details' });
    await detailsLink.click();
  }
}
