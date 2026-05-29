import { Locator, Page } from '@playwright/test';
import { BaseComponent } from './base.component';

export class WriteReviewComponent extends BaseComponent {
  readonly ratingStars: Locator;
  readonly titleInput: Locator;
  readonly bodyInput: Locator;
  readonly submitBtn: Locator;
  readonly cancelBtn: Locator;

  constructor(page: Page) {
    const root = page.locator('app-write-review, [class*="review-form"], form').filter({ has: page.locator('[class*="star"], button') });
    super(page, root);
    this.ratingStars = this.root.locator('button, [role="button"]').filter({ has: this.root.locator('lucide-icon[name="Star"]') });
    this.titleInput = this.root.getByLabel(/title/i).or(this.root.getByPlaceholder(/title/i));
    this.bodyInput = this.root.getByLabel(/review|comment|body/i).or(this.root.getByPlaceholder(/review|comment|write/i));
    this.submitBtn = this.root.getByRole('button', { name: /submit|post|save/i });
    this.cancelBtn = this.root.getByRole('button', { name: /cancel/i });
  }

  async setRating(stars: number) {
    const starBtn = this.ratingStars.nth(stars - 1);
    await starBtn.click();
  }

  async fillTitle(title: string) {
    await this.titleInput.fill(title);
  }

  async fillBody(body: string) {
    await this.bodyInput.fill(body);
  }

  async submit() {
    await this.submitBtn.click();
  }

  async cancel() {
    await this.cancelBtn.click();
  }

  async writeReview(rating: number, title: string, body: string) {
    await this.setRating(rating);
    await this.fillTitle(title);
    await this.fillBody(body);
    await this.submit();
  }
}
