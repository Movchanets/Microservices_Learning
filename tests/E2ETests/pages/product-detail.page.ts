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
  }

  async goto(productId: string) {
    await this.page.goto(`/catalog/${productId}`);
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
