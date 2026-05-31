import { Locator, Page } from '@playwright/test';
import { BaseComponent } from './base.component';

/**
 * Component object for the review summary (average rating, total count, distribution).
 *
 * Scoped to `<app-review-summary>`. Displayed on product detail pages.
 */
export class ReviewSummaryComponent extends BaseComponent {
  readonly averageRating: Locator;
  readonly totalReviews: Locator;
  readonly ratingDistribution: Locator;
  readonly writeReviewBtn: Locator;

  constructor(page: Page) {
    const root = page.locator('app-review-summary');
    super(page, root);

    this.averageRating = this.root.locator('[class*="text-2xl"], [class*="text-3xl"]').first();
    this.totalReviews = this.root.locator('span, p').filter({ hasText: /review/i });
    this.ratingDistribution = this.root.locator('[class*="bar"], [class*="progress"]');
    this.writeReviewBtn = this.root.getByRole('button', { name: /write.*review|add.*review/i });
  }

  async getAverageRating(): Promise<string> {
    return this.averageRating.innerText();
  }

  async getTotalReviewText(): Promise<string> {
    return this.totalReviews.innerText();
  }

  async clickWriteReview() {
    await this.writeReviewBtn.click();
  }
}
