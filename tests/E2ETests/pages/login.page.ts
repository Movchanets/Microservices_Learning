import { Locator, expect, Page } from '@playwright/test';
import { BasePage } from './base.page';

export class LoginPage extends BasePage {
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly loginSubmitBtn: Locator;
  readonly errorMessage: Locator;

  constructor(page: Page) {
    super(page);
    this.emailInput = page.getByTestId('email-input');
    this.passwordInput = page.getByTestId('password-input');
    this.loginSubmitBtn = page.getByTestId('login-submit-btn');
    this.errorMessage = page.locator('role=alert');
  }

  async login(email: string, password: string) {
    for (let attempt = 0; attempt < 3; attempt++) {
      await this.fillStable(this.emailInput, email);
      await this.fillStable(this.passwordInput, password);

      if (await this.loginSubmitBtn.isEnabled()) {
        await this.loginSubmitBtn.click();
        return;
      }
    }

    await expect(this.loginSubmitBtn).toBeEnabled({ timeout: 3000 });
    await this.loginSubmitBtn.click();
  }

  async expectErrorMessage(message: string) {
    await expect(this.errorMessage).toContainText(message);
  }
}
