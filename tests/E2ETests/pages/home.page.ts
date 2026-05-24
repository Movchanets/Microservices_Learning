import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

export class HomePage extends BasePage {
  readonly heroBanner: Locator;
  readonly shopByCategoryHeading: Locator;
  readonly categoryTiles: Locator;
  readonly dealOfTheDay: Locator;
  readonly featuredCarousel: Locator;
  readonly newArrivalsCarousel: Locator;
  readonly recentlyViewedSection: Locator;

  constructor(page: Page) {
    super(page);
    this.heroBanner = page.locator('app-hero-banner');
    this.shopByCategoryHeading = page.getByRole('heading', { name: /shop by category/i });
    this.categoryTiles = page.locator('app-category-tiles');
    this.dealOfTheDay = page.locator('app-deal-of-the-day');
    this.featuredCarousel = page.locator('app-product-carousel').filter({ hasText: /featured/i });
    this.newArrivalsCarousel = page.locator('app-product-carousel').filter({ hasText: /new arrivals/i });
    this.recentlyViewedSection = page.getByRole('heading', { name: /recently viewed/i });
  }

  async goto() {
    await this.page.goto('/home');
    await this.waitForPageLoad();
  }

  async getCategoryTileCount(): Promise<number> {
    const tiles = this.categoryTiles.locator('button, a');
    return tiles.count();
  }

  async clickCategoryTile(name: string) {
    const tile = this.categoryTiles.getByRole('button', { name }).or(this.categoryTiles.getByRole('link', { name }));
    await tile.click();
  }

  async getFeaturedProductCount(): Promise<number> {
    const cards = this.featuredCarousel.locator('app-product-card, [class*="product"]');
    return cards.count();
  }

  async addToCartFromCarousel(index: number) {
    const addBtn = this.featuredCarousel.getByRole('button', { name: /add to cart/i }).nth(index);
    await addBtn.click();
  }

  async isNewArrivalsVisible(): Promise<boolean> {
    return this.newArrivalsCarousel.isVisible();
  }

  async isDealOfTheDayVisible(): Promise<boolean> {
    return this.dealOfTheDay.isVisible();
  }
}
