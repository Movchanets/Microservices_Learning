import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/profile` — user profile settings.
 */
export class ProfilePage extends BasePage {
  readonly logoutBtn: Locator;
  readonly userNameTitle: Locator;
  readonly userEmailText: Locator;

  constructor(page: Page) {
    super(page);
    this.logoutBtn = page.getByTestId('profile-logout-btn');
    this.userNameTitle = page.locator('h1');
    this.userEmailText = page.locator('p:has-text("@")');
  }

  async logout() {
    await this.logoutBtn.click();
  }
}
