import { Page, Locator, expect } from '@playwright/test';
import { HeaderComponent } from '../components/header.component';
import { FooterComponent } from '../components/footer.component';
import { TIMEOUTS } from '../utils/constants';

/**
 * Abstract base for all page objects.
 *
 * Provides:
 * - `url` property — each subclass declares its route
 * - `navigate()` + `waitForPageLoad()` navigation helpers
 * - `waitForAngularReady()` — spinner disappear pattern
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

  // ── Route Declaration ────────────────────────────────────

  /** Each subclass declares its own URL path (relative to BASE_URL). */
  abstract get url(): string;

  // ── Navigation ──────────────────────────────────────────

  /**
   * Navigate to this page's URL and wait for the page to be interactive.
   *
   * For client-rendered pages (authenticated routes), waits for Angular
   * to bootstrap and any loading spinners to resolve.
   *
   * Override `goto()` in subclasses that need extra navigation logic
   * (e.g., tab switching, conditional flows).
   */
  async goto(): Promise<void> {
    await this.page.goto(this.url);
    await this.waitForPageLoad();
  }

  /** Navigate to an arbitrary path (for subclasses that need custom URLs). */
  protected async navigateTo(path: string): Promise<void> {
    await this.page.goto(path);
    await this.waitForPageLoad();
  }

  /**
   * Wait for the page to be interactive.
   *
   * Strategy:
   * 1. Wait for `domcontentloaded` — DOM is parsed
   * 2. Wait for Angular to stabilize — spinners disappear, API calls resolve
   */
  async waitForPageLoad(): Promise<void> {
    await this.page.waitForLoadState('domcontentloaded');
    await this.waitForAngularReady();
  }

  /**
   * Wait for Angular client-side rendering to stabilize.
   *
   * Handles the common pattern where:
   * 1. Angular bootstraps → shows a loading spinner
   * 2. API calls resolve → spinner disappears → real content renders
   *
   * If no spinner appears within 2s, assumes the page loaded instantly.
   */
  async waitForAngularReady(): Promise<void> {
    const spinner = this.page.locator('.animate-spin');

    // If a spinner appears, wait for it to disappear
    const spinnerAppeared = await spinner
      .waitFor({ state: 'visible', timeout: 2_000 })
      .then(() => true)
      .catch(() => false);

    if (spinnerAppeared) {
      await spinner.waitFor({ state: 'hidden', timeout: TIMEOUTS.api });
    }
  }

  /** Assert the browser is currently on this page. */
  async assertOnPage(): Promise<void> {
    await expect(this.page, `Should be on ${this.url}`).toHaveURL(new RegExp(this.url));
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
