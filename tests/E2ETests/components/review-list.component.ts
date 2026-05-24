import { Locator, Page } from '@playwright/test';

export class ReviewListComponent {
  readonly page: Page;
  readonly container: Locator;
  readonly reviews: Locator;
  readonly emptyMessage: Locator;
  readonly loadMoreBtn: Locator;

  constructor(page: Page) {
    this.page = page;
    this.container = page.locator('app-review-list');
    this.reviews = this.container.locator('[class*="border-b"], [class*="divide-y"] > div');
    this.emptyMessage = this.container.getByText(/no reviews|be the first/i);
    this.loadMoreBtn = this.container.getByRole('button', { name: /load more|show more/i });
  }

  async getReviewCount(): Promise<number> {
    return this.reviews.count();
  }

  async getReviewByIndex(index: number): Promise<Locator> {
    return this.reviews.nth(index);
  }

  async getReviewAuthor(index: number): Promise<string> {
    const review = this.reviews.nth(index);
    const author = review.locator('[class*="font-medium"], [class*="font-semibold"]').first();
    return author.innerText();
  }

  async getReviewRating(index: number): Promise<string> {
    const review = this.reviews.nth(index);
    const stars = review.locator('[class*="text-yellow"], lucide-icon[name="Star"]');
    const count = await stars.count();
    return String(count);
  }

  async getReviewText(index: number): Promise<string> {
    const review = this.reviews.nth(index);
    const text = review.locator('p').last();
    return text.innerText();
  }

  async isEmpty(): Promise<boolean> {
    return this.emptyMessage.isVisible();
  }

  async loadMore() {
    if (await this.loadMoreBtn.isVisible()) {
      await this.loadMoreBtn.click();
    }
  }
}
