import { Page, Locator, expect } from '@playwright/test';
import { HeaderComponent } from '../components/header.component';
import { FooterComponent } from '../components/footer.component';

export abstract class BasePage {
  readonly page: Page;
  readonly header: HeaderComponent;
  readonly footer: FooterComponent;

  constructor(page: Page) {
    this.page = page;
    this.header = new HeaderComponent(page);
    this.footer = new FooterComponent(page);
  }

  async goto(path: string) {
    await this.page.goto(path);
  }

  async waitForPageLoad() {
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Fills an input and verifies the value took effect.
   * Retries up to 3 times to handle reactive form interference.
   * Uses expect() polling instead of waitForTimeout for reliability.
   */
  protected async fillStable(input: Locator, value: string): Promise<void> {
    for (let attempt = 0; attempt < 3; attempt++) {
      await input.fill(value);

      try {
        await expect(input).toHaveValue(value, { timeout: 2000 });
        return;
      } catch {
        // Retry on next attempt
      }
    }

    // Final attempt — let the assertion throw if it still fails
    await input.fill(value);
    await expect(input).toHaveValue(value, { timeout: 3000 });
  }

  /**
   * Clicks a submit button with retry logic for reactive forms.
   * Fills all provided inputs via fillStable, then waits for the button
   * to become enabled before clicking. Retries up to 3 times.
   */
  protected async submitWithRetry(
    submitBtn: Locator,
    fields: Array<{ input: Locator; value: string }>
  ): Promise<void> {
    for (let attempt = 0; attempt < 3; attempt++) {
      for (const { input, value } of fields) {
        await this.fillStable(input, value);
      }

      if (await submitBtn.isEnabled()) {
        await submitBtn.click();
        return;
      }
    }

    await expect(submitBtn).toBeEnabled({ timeout: 3000 });
    await submitBtn.click();
  }
}
