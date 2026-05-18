import { Locator, expect, Page } from '@playwright/test';
import { BasePage } from './base.page';

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
    for (let attempt = 0; attempt < 3; attempt++) {
      await this.fillStable(this.firstNameInput, firstName);
      await this.fillStable(this.lastNameInput, lastName);
      await this.fillStable(this.emailInput, email);
      await this.fillStable(this.passwordInput, password);

      if (await this.registerSubmitBtn.isEnabled()) {
        await this.registerSubmitBtn.click();
        return;
      }

      await this.page.waitForTimeout(150);
    }

    await expect(this.registerSubmitBtn).toBeEnabled();
    await this.registerSubmitBtn.click();
  }
}
