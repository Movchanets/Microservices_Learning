import { Locator, Page } from '@playwright/test';

export class WriteReviewComponent {
  readonly page: Page;
  readonly container: Locator;
  readonly ratingStars: Locator;
  readonly titleInput: Locator;
  readonly bodyInput: Locator;
  readonly submitBtn: Locator;
  readonly cancelBtn: Locator;

  constructor(page: Page) {
    this.page = page;
    this.container = page.locator('app-write-review, [class*="review-form"], form').filter({ has: page.locator('[class*="star"], button') });
    this.ratingStars = this.container.locator('button, [role="button"]').filter({ has: page.locator('lucide-icon[name="Star"]') });
    this.titleInput = this.container.getByLabel(/title/i).or(this.container.getByPlaceholder(/title/i));
    this.bodyInput = this.container.getByLabel(/review|comment|body/i).or(this.container.getByPlaceholder(/review|comment|write/i));
    this.submitBtn = this.container.getByRole('button', { name: /submit|post|save/i });
    this.cancelBtn = this.container.getByRole('button', { name: /cancel/i });
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

  async isVisible(): Promise<boolean> {
    return this.container.isVisible();
  }
}
