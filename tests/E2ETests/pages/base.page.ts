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
 * - `submitWithRetry()` — form submit with Angular zoneless compatibility
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
   * Fills form fields and submits. Handles Angular 21 zoneless reactive forms
   * where `fill()` alone doesn't trigger formControlName validity updates.
   *
   * Strategy:
   * 1. `fill()` — sets the native input value atomically
   * 2. `evaluate` dispatches real bubbling `input` + `change` events
   * 3. `press('Tab')` — blur triggers onTouched → change detection
   * 4. Brief wait for Angular zoneless scheduler to process
   */
  protected async submitWithRetry(
    submitBtn: Locator,
    fields: Array<{ input: Locator; value: string }>
  ): Promise<void> {
    for (const { input, value } of fields) {
      await input.click();
      // Use fill() + explicit JS event dispatch for Angular zoneless compatibility
      await input.fill(value);
      await input.evaluate((el) => {
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
        el.dispatchEvent(new Event('blur', { bubbles: true }));
      });
    }

    // Give Angular zoneless scheduler time to run form validity check
    await this.page.waitForTimeout(200);
    await expect(submitBtn).toBeEnabled({ timeout: TIMEOUTS.element });
    await submitBtn.click();
  }
}
