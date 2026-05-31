import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/admin/stores/:id` — store detail view for admins.
 */
export class AdminStoreDetailPage extends BasePage {
  readonly backToVerificationsLink: Locator;
  readonly storeNameHeading: Locator;
  readonly statusBadge: Locator;
  
  // Information lists
  readonly storeInfoList: Locator;
  
  // Verification actions
  readonly approveBtn: Locator;
  readonly rejectBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.backToVerificationsLink = page.getByRole('link', { name: 'Back to Verifications' });
    this.storeNameHeading = page.locator('h2');
    this.statusBadge = page.locator('h2 + span'); // the badge right after the h2
    
    this.storeInfoList = page.locator('dl');
    
    this.approveBtn = page.getByRole('button', { name: 'Approve Store' });
    this.rejectBtn = page.getByRole('button', { name: 'Reject Store' });
  }

  async getStoreInfo(term: string): Promise<string> {
    // Looks for the <dd> following the <dt> containing the term
    const dt = this.storeInfoList.locator('dt').filter({ hasText: term });
    return await dt.locator('~ dd').innerText();
  }

  async approveStore() {
    await this.approveBtn.click();
  }

  async rejectStore(reason: string) {
    // Handle the prompt that asks for rejection reason
    this.page.once('dialog', dialog => dialog.accept(reason));
    await this.rejectBtn.click();
  }

  /** Navigate to a specific store's detail page. */
  async goto(storeId: string) {
    await this.page.goto(`/admin/stores/${storeId}`);
  }
}
