import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/auth/forgot-password` — password reset request.
 */
export class ForgotPasswordPage extends BasePage {
  readonly emailInput: Locator;
  readonly forgotSubmitBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.emailInput = page.getByTestId('email-input');
    this.forgotSubmitBtn = page.getByTestId('forgot-submit-btn');
  }

  get url(): string {
    return '/auth/forgot-password';
  }

  /** Override: wait for Angular hydration after navigation. */
  override async goto(): Promise<void> {
    await this.page.goto(this.url);
    await this.forgotSubmitBtn.waitFor({ state: 'visible', timeout: TIMEOUTS.api });
  }

  async resetPassword(email: string) {
    await this.emailInput.fill(email);
    await this.forgotSubmitBtn.click();
  }
}
