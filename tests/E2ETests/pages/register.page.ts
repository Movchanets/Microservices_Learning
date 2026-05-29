import { Locator, expect, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/auth/register` — registration form.
 */
export class RegisterPage extends BasePage {
  readonly firstNameInput: Locator;
  readonly lastNameInput: Locator;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly registerSubmitBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.firstNameInput = page.getByTestId('first-name-input');
    this.lastNameInput = page.getByTestId('last-name-input');
    this.emailInput = page.getByTestId('email-input');
    this.passwordInput = page.getByTestId('password-input');
    this.registerSubmitBtn = page.getByTestId('register-submit-btn');
  }

  async register(firstName: string, lastName: string, email: string, password: string) {
    await this.submitWithRetry(this.registerSubmitBtn, [
      { input: this.firstNameInput, value: firstName },
      { input: this.lastNameInput, value: lastName },
      { input: this.emailInput, value: email },
      { input: this.passwordInput, value: password },
    ]);
  }
}
