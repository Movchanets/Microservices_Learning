import { Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class ForgotPasswordPage extends BasePage {
  readonly emailInput: Locator;
  readonly forgotSubmitBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.emailInput = page.getByTestId('email-input');
    this.forgotSubmitBtn = page.getByTestId('forgot-submit-btn');
  }

  async resetPassword(email: string) {
    await this.emailInput.fill(email);
    await this.forgotSubmitBtn.click();
  }
}
