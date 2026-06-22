import { Locator, Page } from '@playwright/test';
import { TIMEOUTS } from '../utils/constants';
import { BasePage } from './base.page';

/**
 * Page object for `/auth/login` — email/password login form.
 */
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

  get url(): string {
    return '/auth/login';
  }

  /** Fill email + password and submit the form. */
  async login(email: string, password: string) {
    await this.submitWithRetry(this.loginSubmitBtn, [
      { input: this.emailInput, value: email },
      { input: this.passwordInput, value: password },
    ]);
  }

  /** Wait for an error alert containing `message` to appear. */
  async waitForErrorMessage(message: string, timeout = TIMEOUTS.element) {
    await this.errorMessage.filter({ hasText: message }).waitFor({ state: 'visible', timeout });
  }
}
