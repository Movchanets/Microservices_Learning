import { Locator, Page } from '@playwright/test';

export class NotFoundComponent {
  readonly page: Page;
  readonly heading404: Locator;
  readonly messageText: Locator;
  readonly goHomeLink: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heading404 = page.getByRole('heading', { name: '404' });
    this.messageText = page.getByText('Page not found');
    this.goHomeLink = page.getByRole('link', { name: /go home/i });
  }

  async isVisible(): Promise<boolean> {
    return this.heading404.isVisible();
  }

  async goHome() {
    await this.goHomeLink.click();
  }
}
