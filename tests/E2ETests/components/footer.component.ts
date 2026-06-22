import { Page } from '@playwright/test';
import { BaseComponent } from './base.component';

/**
 * Component object for the site footer.
 */
export class FooterComponent extends BaseComponent {

  constructor(page: Page) {
    const root = page.locator('footer');
    super(page, root);

  }

}