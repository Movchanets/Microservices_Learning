import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';
import { TIMEOUTS } from '../utils/constants';

export interface ProductFormFields {
  name: string;
  description: string;
  brand?: string;
  category: string;
  tags?: string;
}

export interface SkuFormFields {
  skuCode: string;
  price: string;
  currency?: string;
}

/**
 * Page object for seller product create/edit form.
 */
export class ProductFormPage extends BasePage {
  readonly pageHeading: Locator;
  readonly nameInput: Locator;
  readonly descriptionInput: Locator;
  readonly brandInput: Locator;
  readonly categorySelect: Locator;
  readonly tagsInput: Locator;
  readonly submitBtn: Locator;
  readonly addVariantBtn: Locator;
  readonly formErrors: Locator;

  // SKU locators
  readonly skuCodeInput: Locator;
  readonly skuPriceInput: Locator;
  readonly skuCurrencySelect: Locator;
  readonly generateBtn: Locator;

  // Image upload locators
  readonly productImageDropZone: Locator;
  readonly skuImageDropZone: Locator;
  readonly fileInput: Locator;
  readonly skuFileInput: Locator;

  constructor(page: Page) {
    super(page);

    this.pageHeading = page.getByRole('heading', {
      name: /Create Product|Edit Product/,
    });

    this.nameInput = page.getByPlaceholder('e.g. Apple iPhone 17 Pro Max');
    this.descriptionInput = page.locator('textarea');
    this.brandInput = page.getByPlaceholder('e.g. Nike, Apple');
    this.categorySelect = page.locator('select').first();
    this.tagsInput = page.getByPlaceholder('electronics, gadgets, wireless');

    this.submitBtn = page.getByRole('button', {
      name: /Create Product|Update Product/,
    });
    this.addVariantBtn = page.getByRole('button', { name: /Add Variant/ });

    this.formErrors = page.locator('.bg-red-500\\/10 p');

    this.skuCodeInput = page.locator('input[placeholder="e.g. WIDGET-001"]');
    this.skuPriceInput = page.locator('input[type="number"]');
    this.skuCurrencySelect = page.locator('select').last();
    this.generateBtn = page.getByRole('button', { name: 'Generate' });

    this.productImageDropZone = page
      .locator('app-image-gallery-uploader')
      .first()
      .locator('[class*="border-dashed"]');
    this.skuImageDropZone = page
      .locator('app-image-gallery-uploader')
      .last()
      .locator('[class*="border-dashed"]');
    this.fileInput = page.locator('input[type="file"]').first();
    this.skuFileInput = page.locator('input[type="file"]').last();
  }

  get url(): string {
    return '/seller/products/new';
  }

  async fillProductInfo(fields: ProductFormFields) {
    await this.nameInput.fill(fields.name);
    await this.nameInput.dispatchEvent('input');
    await this.descriptionInput.fill(fields.description);
    await this.descriptionInput.dispatchEvent('input');
    if (fields.brand) {
      await this.brandInput.fill(fields.brand);
      await this.brandInput.dispatchEvent('input');
    }
    await this.categorySelect.selectOption({ label: fields.category });
    if (fields.tags) {
      await this.tagsInput.fill(fields.tags);
      await this.tagsInput.dispatchEvent('input');
    }
  }

  async addVariant() {
    await this.addVariantBtn.click();
  }

  async switchToSkuTab(index: number) {
    const tabButtons = this.page.locator('button:has-text("Variant")');
    await tabButtons.nth(index).click();
  }

  async fillSkuInfo(index: number, fields: SkuFormFields) {
    await this.switchToSkuTab(index);
    await this.skuCodeInput.fill(fields.skuCode);
    await this.skuCodeInput.dispatchEvent('input');
    await this.skuPriceInput.fill(fields.price);
    await this.skuPriceInput.dispatchEvent('input');
    if (fields.currency) {
      await this.skuCurrencySelect.selectOption(fields.currency);
    }
  }

  async generateSkuCode(index: number) {
    await this.switchToSkuTab(index);
    await this.generateBtn.click();
  }

  async uploadProductImages(filePaths: string[]) {
    await this.fileInput.setInputFiles(filePaths);
    // Wait for image preview to appear (Angular processes the file)
    await this.page.locator('app-image-gallery-uploader').first()
      .locator('img').first().waitFor({ state: 'visible', timeout: TIMEOUTS.quick });
  }

  async uploadSkuImages(skuIndex: number, filePaths: string[]) {
    await this.switchToSkuTab(skuIndex);
    await this.skuFileInput.setInputFiles(filePaths);
    // Wait for image preview to appear
    await this.page.locator('app-image-gallery-uploader').last()
      .locator('img').first().waitFor({ state: 'visible', timeout: TIMEOUTS.quick });
  }

  async submit() {
    await this.submitBtn.click();
  }

  async waitForSuccess() {
    await this.page.waitForURL('**/seller/products', { timeout: TIMEOUTS.api });
  }

  async getFormErrors(): Promise<string[]> {
    return this.formErrors.allTextContents();
  }

  async getImageCount(): Promise<number> {
    return this.page
      .locator('app-image-gallery-uploader')
      .first()
      .locator('img')
      .count();
  }

  async getSkuImageCount(): Promise<number> {
    return this.page
      .locator('app-image-gallery-uploader')
      .last()
      .locator('img')
      .count();
  }
}