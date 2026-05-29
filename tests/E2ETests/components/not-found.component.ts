import { Locator, Page } from '@playwright/test';
import { BaseComponent } from './base.component';

/**
 * Component object for the 404 page.
 */
export class NotFoundComponent extends BaseComponent {
  readonly heading404: Locator;
  readonly messageText: Locator;
  readonly goHomeLink: Locator;

  constructor(page: Page) {
    const root = page.locator('app-not-found');
    super(page, root);
    this.heading404 = this.root.getByRole('heading', { name: '404' });
    this.messageText = this.root.getByText('Page not found');
    this.goHomeLink = this.root.getByRole('link', { name: /go home/i });
  }

  override async isVisible(): Promise<boolean> {
    return this.heading404.isVisible();
  }

  async goHome() {
    await this.goHomeLink.click();
  }
}
