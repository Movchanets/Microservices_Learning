import { Page, Locator } from '@playwright/test';
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
    await this.page.waitForLoadState('networkidle');
  }
}
