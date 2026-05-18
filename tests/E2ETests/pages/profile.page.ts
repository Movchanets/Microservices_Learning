import { Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

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

  async expectUserDetails(firstName: string, lastName: string, email: string) {
    await expect(this.userNameTitle).toContainText(firstName);
    await expect(this.userNameTitle).toContainText(lastName);
    await expect(this.page.locator('body')).toContainText(email);
  }
}
