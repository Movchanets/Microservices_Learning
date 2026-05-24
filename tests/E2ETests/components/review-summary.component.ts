import { Locator, Page } from '@playwright/test';

export class ReviewSummaryComponent {
  readonly page: Page;
  readonly container: Locator;
  readonly averageRating: Locator;
  readonly totalReviews: Locator;
  readonly ratingDistribution: Locator;
  readonly writeReviewBtn: Locator;

  constructor(page: Page) {
    this.page = page;
    this.container = page.locator('app-review-summary');
    this.averageRating = this.container.locator('[class*="text-2xl"], [class*="text-3xl"]').first();
    this.totalReviews = this.container.locator('span, p').filter({ hasText: /review/i });
    this.ratingDistribution = this.container.locator('[class*="bar"], [class*="progress"]');
    this.writeReviewBtn = this.container.getByRole('button', { name: /write.*review|add.*review/i });
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

  async isVisible(): Promise<boolean> {
    return this.container.isVisible();
  }
}
