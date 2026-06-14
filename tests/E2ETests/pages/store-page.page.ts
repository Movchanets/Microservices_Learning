import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/store/:slug` — public store page.
 */
export class StorePagePage extends BasePage {
  readonly storeNameHeading: Locator;
  readonly storeDescription: Locator;
  readonly verifiedBadge: Locator;
  readonly backToCatalogLink: Locator;
  readonly productsGrid: Locator;
  readonly productCards: Locator;
  readonly emptyProductsMessage: Locator;
  readonly loadingSkeleton: Locator;
  readonly errorState: Locator;
  readonly errorReturnLink: Locator;

  constructor(page: Page) {
    super(page);
    this.storeNameHeading = page.getByRole('heading', { level: 1 });
    this.storeDescription = page.locator('app-store-page p').filter({ hasText: /.+/ });
    this.verifiedBadge = page.getByText(/verified seller/i);
    this.backToCatalogLink = page.getByRole('link', { name: /back to catalog/i });
    this.productsGrid = page.locator('.grid');
    this.productCards = page.locator('app-product-card');
    this.emptyProductsMessage = page.getByText(/no products from this store/i);
    this.loadingSkeleton = page.locator('.animate-pulse');
    this.errorState = page.locator('.text-red-400');
    this.errorReturnLink = page.getByRole('link', { name: /return to catalog/i });
  }

  get url(): string {
    return '/stores';
  }


  async goto(storeId: string) {
    await this.page.goto(`/stores/${storeId}`);
    await this.waitForPageLoad();
  }

  async getStoreName(): Promise<string> {
    return this.storeNameHeading.innerText();
  }

  async isVerified(): Promise<boolean> {
    return this.verifiedBadge.isVisible();
  }

  async getProductCount(): Promise<number> {
    return this.productCards.count();
  }

  async clickProduct(index: number) {
    await this.productCards.nth(index).click();
  }

  async isLoading(): Promise<boolean> {
    return this.loadingSkeleton.isVisible();
  }

  async hasError(): Promise<boolean> {
    return this.errorState.isVisible();
  }

  async getErrorMessage(): Promise<string> {
    return this.errorState.innerText();
  }

  async goToCatalog() {
    await this.backToCatalogLink.click();
  }
}
