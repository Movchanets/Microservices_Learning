import { Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class LoginPage extends BasePage {
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly loginSubmitBtn: Locator;
  readonly errorMessage: Locator;

  constructor(page: any) {
    super(page);
    this.emailInput = page.getByTestId('email-input');
    this.passwordInput = page.getByTestId('password-input');
    this.loginSubmitBtn = page.getByTestId('login-submit-btn');
    this.errorMessage = page.locator('role=alert');
  }

  private async fillStable(input: Locator, value: string) {
    for (let attempt = 0; attempt < 3; attempt++) {
      await input.fill(value);
      await expect(input).toHaveValue(value);

      if ((await input.inputValue()) === value) {
        return;
      }

      await this.page.waitForTimeout(100);
    }
  }

  async login(email: string, password: string) {
    for (let attempt = 0; attempt < 3; attempt++) {
      await this.fillStable(this.emailInput, email);
      await this.fillStable(this.passwordInput, password);

      if (await this.loginSubmitBtn.isEnabled()) {
        await this.loginSubmitBtn.click();
        return;
      }

      await this.page.waitForTimeout(150);
    }

    await expect(this.loginSubmitBtn).toBeEnabled();
    await this.loginSubmitBtn.click();
  }

  async expectErrorMessage(message: string) {
    await expect(this.errorMessage).toContainText(message);
  }
}
