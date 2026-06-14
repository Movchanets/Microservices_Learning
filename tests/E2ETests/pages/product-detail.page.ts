import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/catalog/:id` — product detail page.
 */
export class ProductDetailPage extends BasePage {
  readonly productName: Locator;
  readonly productPrice: Locator;
  readonly productDescription: Locator;
  readonly productSku: Locator;
  readonly addToCartBtn: Locator;
  readonly backToCatalogLink: Locator;
  readonly productImage: Locator;
  readonly quantityInput: Locator;

  // Variant picker
  readonly variantPicker: Locator;
  readonly variantBreadcrumb: Locator;

  // Gallery
  readonly galleryMainImage: Locator;
  readonly galleryThumbnails: Locator;

  // Specs table
  readonly specsSection: Locator;
  readonly specsHeading: Locator;

  constructor(page: Page) {
    super(page);
    this.productName = page.getByTestId('product-name');
    this.productPrice = page.getByTestId('product-price');
    this.productDescription = page.getByTestId('product-description');
    this.productSku = page.getByTestId('product-sku');
    this.addToCartBtn = page.getByRole('button', { name: /add to cart/i });
    this.backToCatalogLink = page.getByRole('link', { name: /back to catalog/i });
    this.productImage = page.getByTestId('product-image');
    this.quantityInput = page.getByTestId('quantity-input');

    // Variant picker — rendered by <app-variant-picker>
    this.variantPicker = page.locator('app-variant-picker');
    this.variantBreadcrumb = page.locator('[class*="bg-primary/10"]').filter({ hasText: /·/ });

    // Gallery — rendered by <app-image-gallery>
    this.galleryMainImage = page.locator('app-image-gallery img').first();
    this.galleryThumbnails = page.locator('app-image-gallery button[aria-label^="View"]');

    // Specs table
    this.specsHeading = page.getByRole('heading', { name: 'Specifications' });
    this.specsSection = this.specsHeading.locator('..');
  }

  get url(): string {
    return '/catalog';
  }


  async goto(productId: string) {
    await this.page.goto(`/catalog/${productId}`);
  }

  /**
   * Returns a locator for a specific variant button.
   * Uses the data-testid pattern: variant-{axisKey}-{value}
   */
  getVariantButton(axisKey: string, value: string): Locator {
    return this.page.getByTestId(`variant-${axisKey}-${value}`);
  }

  /**
   * Selects a variant value for a given axis.
   */
  async selectVariant(axisKey: string, value: string): Promise<void> {
    await this.getVariantButton(axisKey, value).click();
    // Wait for Angular change detection to propagate
    await this.page.waitForTimeout(300);
  }

  /**
   * Returns the currently displayed price text.
   */
  async getPriceText(): Promise<string> {
    // Price is inside the buy-box component
    return this.page.locator('app-buy-box .text-3xl').innerText();
  }

  /**
   * Returns the variant breadcrumb text (e.g. "Gold · 512GB").
   */
  async getVariantBreadcrumbText(): Promise<string | null> {
    const el = this.variantBreadcrumb;
    if (await el.isVisible().catch(() => false)) {
      return el.innerText();
    }
    return null;
  }

  /**
   * Returns all spec row labels from the specifications table.
   */
  async getSpecLabels(): Promise<string[]> {
    const rows = this.specsSection.locator('[class*="flex items-center px-4"]');
    const count = await rows.count();
    const labels: string[] = [];
    for (let i = 0; i < count; i++) {
      const label = await rows.nth(i).locator('span').first().innerText();
      labels.push(label);
    }
    return labels;
  }

  /**
   * Returns the main gallery image src.
   */
  async getMainImageSrc(): Promise<string | null> {
    if (await this.galleryMainImage.isVisible().catch(() => false)) {
      return this.galleryMainImage.getAttribute('src');
    }
    return null;
  }

  /**
   * Returns the count of gallery thumbnail images.
   */
  async getGalleryThumbnailCount(): Promise<number> {
    return this.galleryThumbnails.count();
  }

  async addToCart() {
    await this.addToCartBtn.click();
  }

  async getName(): Promise<string> {
    return await this.productName.innerText();
  }

  async getPrice(): Promise<string> {
    return await this.productPrice.innerText();
  }
}
