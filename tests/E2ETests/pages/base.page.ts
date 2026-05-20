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
   */
  protected async fillStable(input: Locator, value: string): Promise<void> {
    for (let attempt = 0; attempt < 3; attempt++) {
      await input.fill(value);
      await expect(input).toHaveValue(value);

      if ((await input.inputValue()) === value) {
        return;
      }

      await this.page.waitForTimeout(100);
    }
  }
}
