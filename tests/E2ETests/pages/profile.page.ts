import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/profile` — user profile settings.
 */
export class ProfilePage extends BasePage {
  // ── Profile Info ────────────────────────────────────────
  readonly heading: Locator;
  readonly logoutBtn: Locator;
  readonly userNameTitle: Locator;
  readonly userEmailText: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole('heading', { name: /profile/i });
    this.logoutBtn = page.getByRole('button', { name: /sign out/i });
    this.userNameTitle = page.locator('h1');
    this.userEmailText = page.locator('p:has-text("@")');
  }

  // ── Actions ─────────────────────────────────────────────

  get url(): string {
    return '/profile';
  }

  async logout() {
    await this.logoutBtn.click();
  }
}
