import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';
import { CartDrawerComponent } from '../components/cart-drawer.component';

/**
 * Page object for `/home` — landing page with featured products and categories.
 */
export class HomePage extends BasePage {
  // ── Components ──────────────────────────────────────────
  readonly cartDrawer: CartDrawerComponent;

  // ── Locators ────────────────────────────────────────────
  readonly shopByCategoryHeading: Locator;
  readonly categoryTiles: Locator;
  readonly featuredCarousel: Locator;
  readonly newArrivalsCarousel: Locator;
  readonly recentlyViewedSection: Locator;

  constructor(page: Page) {
    super(page);
    this.cartDrawer = new CartDrawerComponent(page);
    this.shopByCategoryHeading = page.getByRole('heading', { name: /shop by category/i });
    this.categoryTiles = page.locator('app-category-tiles');
    this.featuredCarousel = page.locator('app-product-carousel').filter({ hasText: /featured/i });
    this.newArrivalsCarousel = page.locator('app-product-carousel').filter({ hasText: /new arrivals/i });
    this.recentlyViewedSection = page.getByRole('heading', { name: /recently viewed/i });
  }

  get url(): string {
    return '/home';
  }

  async getCategoryTileCount(): Promise<number> {
    const tiles = this.categoryTiles.locator('button, a');
    return tiles.count();
  }

  async clickCategoryTile(nameOrIndex: string | number) {
    if (typeof nameOrIndex === 'number') {
      const tile = this.categoryTiles.locator('a, button').nth(nameOrIndex);
      await tile.click();
    } else {
      const tile = this.categoryTiles.getByRole('button', { name: nameOrIndex }).or(this.categoryTiles.getByRole('link', { name: nameOrIndex }));
      await tile.click();
    }
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
