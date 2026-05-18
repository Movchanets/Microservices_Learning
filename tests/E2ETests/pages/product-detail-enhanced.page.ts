import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class ProductDetailEnhancedPage extends BasePage {
  readonly productName: Locator;
  readonly productPrice: Locator;
  readonly productDescription: Locator;
  readonly productSku: Locator;
  readonly productImage: Locator;
  readonly backToCatalogLink: Locator;

  // Buy box
  readonly buyBox: Locator;
  readonly quantityInput: Locator;
  readonly addToCartBtn: Locator;
  readonly stockIndicator: Locator;

  // Reviews
  readonly reviewSummary: Locator;
  readonly averageRating: Locator;
  readonly reviewCount: Locator;
  readonly writeReviewBtn: Locator;
  readonly reviewList: Locator;
  readonly reviewItems: Locator;

  // Frequently Bought Together
  readonly frequentlyBoughtTogether: Locator;
  readonly bundleItems: Locator;
  readonly addBundleToCartBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.productName = page.getByTestId('product-name');
    this.productPrice = page.getByTestId('product-price');
    this.productDescription = page.getByTestId('product-description');
    this.productSku = page.getByTestId('product-sku');
    this.productImage = page.getByTestId('product-image');
    this.backToCatalogLink = page.getByRole('link', { name: /back to catalog/i });

    // Buy box
    this.buyBox = page.locator('app-buy-box');
    this.quantityInput = page.getByTestId('quantity-input').or(this.buyBox.locator('input[type="number"]'));
    this.addToCartBtn = page.getByRole('button', { name: /add to cart/i });
    this.stockIndicator = page.locator('app-stock-indicator');

    // Reviews
    this.reviewSummary = page.locator('app-review-summary');
    this.averageRating = this.reviewSummary.getByTestId('average-rating');
    this.reviewCount = this.reviewSummary.locator('span, p').filter({ hasText: /review/i });
    this.writeReviewBtn = page.getByRole('button', { name: /write.*review|add.*review/i });
    this.reviewList = page.locator('app-review-list');
    this.reviewItems = this.reviewList.locator('[class*="border-b"], [class*="divide-y"] > *');

    // Frequently Bought Together
    this.frequentlyBoughtTogether = page.locator('app-frequently-bought-together');
    this.bundleItems = this.frequentlyBoughtTogether.locator('[class*="flex"]').filter({ has: page.locator('img, input[type="checkbox"]') });
    this.addBundleToCartBtn = this.frequentlyBoughtTogether.getByRole('button', { name: /add.*to cart|add selected/i });
  }

  async goto(productId: string) {
    await this.page.goto(`/catalog/${productId}`);
  }

  async addToCart() {
    await this.addToCartBtn.click();
  }

  async setQuantity(qty: number) {
    await this.quantityInput.fill(String(qty));
  }

  async increaseQuantity() {
    await this.buyBox.locator('button').filter({ has: this.page.locator('lucide-icon[name="Plus"]') }).click();
  }

  async decreaseQuantity() {
    await this.buyBox.locator('button').filter({ has: this.page.locator('lucide-icon[name="Minus"]') }).click();
  }

  async getStockText(): Promise<string> {
    return this.stockIndicator.innerText();
  }

  async isOutOfStock(): Promise<boolean> {
    const text = await this.getStockText();
    return text.toLowerCase().includes('out of stock');
  }

  async isLowStock(): Promise<boolean> {
    const text = await this.getStockText();
    return text.toLowerCase().includes('low stock');
  }

  async isInStock(): Promise<boolean> {
    const text = await this.getStockText();
    return text.toLowerCase().includes('in stock');
  }

  async getReviewCount(): Promise<number> {
    return this.reviewItems.count();
  }

  async writeReview() {
    await this.writeReviewBtn.click();
  }

  async addBundleToCart() {
    await this.addBundleToCartBtn.click();
  }

  async getName(): Promise<string> {
    return this.productName.innerText();
  }

  async getPrice(): Promise<string> {
    return this.productPrice.innerText();
  }
}
