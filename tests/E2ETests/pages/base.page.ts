import { Page, Locator, expect } from '@playwright/test';
import { HeaderComponent } from '../components/header.component';
import { FooterComponent } from '../components/footer.component';
import { TIMEOUTS } from '../utils/constants';

/**
 * Abstract base for all page objects.
 *
 * Provides:
 * - Shared header / footer components
 * - `goto()` + `waitForPageLoad()` navigation helpers
 * - `fillStable()` — Angular-safe input fill with retry
 * - `submitWithRetry()` — form submit with fill + enable-check loop
 */
export abstract class BasePage {
  readonly page: Page;
  readonly header: HeaderComponent;
  readonly footer: FooterComponent;

  constructor(page: Page) {
    this.page = page;
    this.header = new HeaderComponent(page);
    this.footer = new FooterComponent(page);
  }

  // ── Navigation ──────────────────────────────────────────

  /** Navigate to a route (relative to BASE_URL). */
  async goto(path: string) {
    await this.page.goto(path);
  }

  /** Wait for DOMContentLoaded. Prefer `expect(header.logo).toBeVisible()` for Angular SSR pages. */
  async waitForPageLoad() {
    await this.page.waitForLoadState('domcontentloaded');
  }

  // ── Form Helpers ────────────────────────────────────────

  /**
   * Fills an input and dispatches 'input' event for Angular signal-based forms.
   * Retries up to 3 times to handle reactive form interference.
   */
  protected async fillStable(input: Locator, value: string): Promise<void> {
    for (let attempt = 0; attempt < 3; attempt++) {
      await input.fill(value);
      await input.dispatchEvent('input');

      try {
        await expect(input).toHaveValue(value, { timeout: TIMEOUTS.fillRetry });
        return;
      } catch {
        // Retry on next attempt
      }
    }

    // Final attempt — let the assertion throw if it still fails
    await input.fill(value);
    await input.dispatchEvent('input');
    await expect(input).toHaveValue(value, { timeout: TIMEOUTS.quick });
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

    await expect(submitBtn).toBeEnabled({ timeout: TIMEOUTS.quick });
    await submitBtn.click();
  }
}
