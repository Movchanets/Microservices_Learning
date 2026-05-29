/**
import { TIMEOUTS } from '../../utils/constants';
 * E2E Tests: Product Creation with SKU Galleries & Image Uploads
 *
 * Tests the full seller product creation workflow including:
 *   1. Creating products with multiple SKU variants via API
 *   2. Uploading images to product-level gallery via API
 *   3. Uploading per-SKU images via API
 *   4. Creating products with SKUs and images via UI form
 *   5. Gallery management (delete, reorder, set primary)
 *   6. Form validation (missing SKUs, empty codes, duplicate codes)
 *
 * Uses the auth fixture for pre-authenticated seller/admin API contexts
 * and browser contexts.
 */

import { authTest as test, expect } from '../../fixtures/auth.fixture';
import {
  createProduct,
  addSku,
  getProductById,
  ensureCategoryExists,
} from '../../utils/catalog-helpers';
import { ensureStoreExists } from '../../utils/store-helpers';
import {
  uploadMedia,
  getGallery,
  deleteMedia,
  setPrimaryMedia,
  getTestImagePath,
} from '../../utils/media-helpers';
import { ProductFormPage } from '../../pages/product-form.page';
import type { StoreResult, ProductResult, SkuResult } from '../../utils/types';

// ── Shared Setup ──────────────────────────────────────────

test.describe('Seller: Product Creation with SKU Galleries', () => {
  let store: StoreResult;
  let categoryId: string;
  const uniqueId = Math.random().toString(36).substring(7).toUpperCase();

  test.beforeAll(async ({ sellerApi, sellerUser, adminApi }) => {
    // Create and verify a store for the seller
    store = await ensureStoreExists(sellerApi, adminApi, sellerUser.id, `Gallery Store ${uniqueId}`, 'E2E gallery test store');

    // Ensure a category exists
    const category = await ensureCategoryExists(adminApi, 'Electronics', 'Devices and gadgets');
    categoryId = category.id;
  });

  // ── Scenario 1: Create product with multiple SKU variants via API ──

  test('should create product with 3 SKU variants via API', async ({ sellerApi }) => {
    let product: ProductResult;

    await test.step('Create a product', async () => {
      product = await createProduct(sellerApi, {
        name: `Gallery Phone ${uniqueId}`,
        description: 'Smartphone with multiple color and storage variants',
        categoryId,
        storeId: store.id,
        brand: 'TestBrand',
        tags: ['phone', 'e2e', 'gallery-test'],
      });
      expect(product).toBeTruthy();
      expect(product.id).toBeTruthy();
    });

    await test.step('Add 3 SKUs with different codes', async () => {
      const sku1 = await addSku(sellerApi, product.id, {
        skuCode: `PHONE-RED-256-${uniqueId}`,
        price: 999.99,
        currency: 'USD',
        typedAttributes: { color: 'Red', storage: '256GB' },
      });
      expect(sku1.id).toBeTruthy();
      expect(sku1.skuCode).toBe(`PHONE-RED-256-${uniqueId}`);
      expect(sku1.price).toBe(999.99);

      const sku2 = await addSku(sellerApi, product.id, {
        skuCode: `PHONE-BLUE-512-${uniqueId}`,
        price: 1199.99,
        currency: 'USD',
        typedAttributes: { color: 'Blue', storage: '512GB' },
      });
      expect(sku2.skuCode).toBe(`PHONE-BLUE-512-${uniqueId}`);
      expect(sku2.price).toBe(1199.99);

      const sku3 = await addSku(sellerApi, product.id, {
        skuCode: `PHONE-BLACK-1TB-${uniqueId}`,
        price: 1499.99,
        currency: 'USD',
        typedAttributes: { color: 'Black', storage: '1TB' },
      });
      expect(sku3.skuCode).toBe(`PHONE-BLACK-1TB-${uniqueId}`);
      expect(sku3.price).toBe(1499.99);
    });

    await test.step('Verify product has 3 SKUs with correct data', async () => {
      const fetched = await getProductById(sellerApi, product.id);
      expect(fetched).toBeTruthy();
      expect(fetched!.skus).toHaveLength(3);

      const skuCodes = fetched!.skus.map((s) => s.skuCode);
      expect(skuCodes).toContain(`PHONE-RED-256-${uniqueId}`);
      expect(skuCodes).toContain(`PHONE-BLUE-512-${uniqueId}`);
      expect(skuCodes).toContain(`PHONE-BLACK-1TB-${uniqueId}`);

      const redSku = fetched!.skus.find((s) => s.skuCode === `PHONE-RED-256-${uniqueId}`);
      expect(redSku!.price).toBe(999.99);
      expect(redSku!.currency).toBe('USD');

      const blueSku = fetched!.skus.find((s) => s.skuCode === `PHONE-BLUE-512-${uniqueId}`);
      expect(blueSku!.price).toBe(1199.99);

      const blackSku = fetched!.skus.find((s) => s.skuCode === `PHONE-BLACK-1TB-${uniqueId}`);
      expect(blackSku!.price).toBe(1499.99);
    });
  });

  // ── Scenario 2: Upload images to product gallery via API ──

  test('should upload multiple images to product gallery and manage primary', async ({ sellerApi }) => {
    let product: ProductResult;

    await test.step('Create a product', async () => {
      product = await createProduct(sellerApi, {
        name: `Gallery Images Product ${uniqueId}`,
        description: 'Product for testing image gallery management',
        categoryId,
        storeId: store.id,
      });
    });

    await test.step('Upload 3 product-level images', async () => {
      await uploadMedia(sellerApi, getTestImagePath('product-1.jpg'), product.id, 'Product', true);
      await uploadMedia(sellerApi, getTestImagePath('product-2.jpg'), product.id, 'Product', false);
      await uploadMedia(sellerApi, getTestImagePath('product-3.jpg'), product.id, 'Product', false);
    });

    await test.step('Verify gallery has 3 images and first is primary', async () => {
      const gallery = await getGallery(sellerApi, 'Product', product.id);
      expect(gallery).toHaveLength(3);

      const primaryImage = gallery.find((img) => img.isPrimary === true);
      expect(primaryImage).toBeTruthy();
      // First uploaded image should be the primary one
      expect(primaryImage!.fileName).toContain('product-1');
    });

    await test.step('Set second image as primary', async () => {
      const gallery = await getGallery(sellerApi, 'Product', product.id);
      const secondImage = gallery.find((img) => img.fileName.includes('product-2'));
      expect(secondImage).toBeTruthy();

      await setPrimaryMedia(sellerApi, 'Product', product.id, secondImage!.id);
    });

    await test.step('Verify primary changed to second image', async () => {
      const gallery = await getGallery(sellerApi, 'Product', product.id);
      const primaryImage = gallery.find((img) => img.isPrimary === true);
      expect(primaryImage).toBeTruthy();
      expect(primaryImage!.fileName).toContain('product-2');
    });
  });

  // ── Scenario 3: Upload per-SKU images via API ──

  test('should upload per-SKU images and verify separate galleries', async ({ sellerApi }) => {
    let product: ProductResult;
    let sku1: SkuResult;
    let sku2: SkuResult;

    await test.step('Create product with 2 SKUs', async () => {
      product = await createProduct(sellerApi, {
        name: `SKU Gallery Product ${uniqueId}`,
        description: 'Product with per-SKU image galleries',
        categoryId,
        storeId: store.id,
      });

      sku1 = await addSku(sellerApi, product.id, {
        skuCode: `SKU-RED-${uniqueId}`,
        price: 49.99,
        currency: 'USD',
        typedAttributes: { color: 'Red' },
      });

      sku2 = await addSku(sellerApi, product.id, {
        skuCode: `SKU-BLUE-${uniqueId}`,
        price: 59.99,
        currency: 'USD',
        typedAttributes: { color: 'Blue' },
      });
    });

    await test.step('Upload 2 images to SKU 1', async () => {
      await uploadMedia(sellerApi, getTestImagePath('sku-red-1.jpg'), sku1.id, 'SKU', true);
      // Reuse product-1.jpg as second SKU image
      await uploadMedia(sellerApi, getTestImagePath('product-1.jpg'), sku1.id, 'SKU', false);
    });

    await test.step('Upload 1 image to SKU 2', async () => {
      await uploadMedia(sellerApi, getTestImagePath('sku-blue-1.jpg'), sku2.id, 'SKU', true);
    });

    await test.step('Verify SKU 1 gallery has 2 images', async () => {
      const gallery1 = await getGallery(sellerApi, 'SKU', sku1.id);
      expect(gallery1).toHaveLength(2);
      const primary1 = gallery1.find((img) => img.isPrimary === true);
      expect(primary1).toBeTruthy();
      expect(primary1!.fileName).toContain('sku-red-1');
    });

    await test.step('Verify SKU 2 gallery has 1 image', async () => {
      const gallery2 = await getGallery(sellerApi, 'SKU', sku2.id);
      expect(gallery2).toHaveLength(1);
      expect(gallery2[0].isPrimary).toBe(true);
      expect(gallery2[0].fileName).toContain('sku-blue-1');
    });
  });

  // ── Scenario 4: Create product with SKUs and images via UI ──
  // Need to either: (a) create store via UI first, or (b) refresh store state after API creation.

  // TODO: Angular signal-based inputs resist Playwright's fill/type/dispatchEvent.
  // Need to either: (a) use page.evaluate with Angular's NgZone, or (b) add data-testid
  // attributes with explicit event dispatch in the Angular component.
  test.skip('should create product with SKUs and images via UI form', async ({ sellerContext, sellerUser, adminApi }) => {
    const page = await sellerContext.newPage();
    const productForm = new ProductFormPage(page);

    // Ensure category for UI selection
    const category = await test.step('Ensure category exists for UI', async () => {
      return ensureCategoryExists(adminApi, 'Electronics', 'Devices and gadgets');
    });

    await test.step('Ensure seller has a store', async () => {
      await page.goto('/seller');
      await page.waitForLoadState('domcontentloaded');

      const createStoreHeading = page.getByRole('heading', { name: 'Create Your Store' });
      const hasCreateForm = await createStoreHeading.isVisible({ timeout: TIMEOUTS.quick }).catch(() => false);

      if (hasCreateForm) {
        const storeNameInput = page.getByTestId('store-name-input');
        const storeDescInput = page.getByPlaceholder('Tell customers what your store is about...');
        const createStoreBtn = page.getByRole('button', { name: 'Create Store' });

        await expect(storeNameInput).toBeVisible({ timeout: TIMEOUTS.quick });

        // Clear and type into store name
        await storeNameInput.click({ clickCount: 3 });
        await storeNameInput.pressSequentially('E2E Store ' + uniqueId, { delay: 20 });
        // Clear and type into description
        await storeDescInput.click({ clickCount: 3 });
        await storeDescInput.pressSequentially('Automated E2E test store for product creation', { delay: 20 });
        await page.waitForTimeout(500);

        await createStoreBtn.click({ timeout: TIMEOUTS.element });
        await page.waitForTimeout(3000);
      }
    });

    await test.step('Navigate to product creation form', async () => {
      await page.goto('/seller/products/new');
      await page.waitForLoadState('domcontentloaded');
      await expect(productForm.pageHeading).toBeVisible({ timeout: TIMEOUTS.api });
    });

    await test.step('Fill product info', async () => {
      await productForm.fillProductInfo({
        name: `UI Gallery Product ${uniqueId}`,
        description: 'Product created via UI form with image uploads',
        brand: 'UITestBrand',
        category: 'Electronics',
        tags: 'e2e, gallery, ui-test',
      });
    });

    await test.step('Upload 2 product images', async () => {
      await productForm.uploadProductImages([
        getTestImagePath('product-1.jpg'),
        getTestImagePath('product-2.jpg'),
      ]);
    });

    await test.step('Fill SKU variant', async () => {
      await productForm.fillSkuInfo(0, {
        skuCode: `UI-SKU-${uniqueId}`,
        price: '79.99',
        currency: 'USD',
      });
      await productForm.uploadSkuImages(0, [getTestImagePath('sku-red-1.jpg')]);
    });

    await test.step('Submit the form', async () => {
      await productForm.submit();
      // Wait for either success redirect or error messages
      try {
        await productForm.waitForSuccess();
      } catch {
        // If redirect didn't happen, check for errors
        const errors = await productForm.getFormErrors();
        if (errors.length > 0) {
          throw new Error(`Form submission failed with errors: ${errors.join(', ')}`);
        }
        throw new Error('Form submission did not redirect and no errors shown');
      }
    });

    await test.step('Verify redirect to products list', async () => {
      await expect(page).toHaveURL(/\/seller\/products/);
    });

    await test.step('Verify product was created', async () => {
      // If we got here, the form submitted successfully and redirected
      await expect(page).toHaveURL(/\/seller\/products/);
    });

    await page.close();
  });

  // ── Scenario 5: Gallery management — delete, reorder, set primary ──

  test('should manage gallery: delete image and change primary', async ({ sellerApi }) => {
    let product: ProductResult;

    await test.step('Create product with 3 images', async () => {
      product = await createProduct(sellerApi, {
        name: `Gallery Mgmt Product ${uniqueId}`,
        description: 'Product for testing gallery management operations',
        categoryId,
        storeId: store.id,
      });

      await uploadMedia(sellerApi, getTestImagePath('product-1.jpg'), product.id, 'Product', true);
      await uploadMedia(sellerApi, getTestImagePath('product-2.jpg'), product.id, 'Product', false);
      await uploadMedia(sellerApi, getTestImagePath('product-3.jpg'), product.id, 'Product', false);
    });

    let gallery: Awaited<ReturnType<typeof getGallery>>;

    await test.step('Verify initial gallery has 3 images', async () => {
      gallery = await getGallery(sellerApi, 'Product', product.id);
      expect(gallery).toHaveLength(3);
    });

    await test.step('Delete the third image', async () => {
      const thirdImage = gallery.find((img) => img.fileName.includes('product-3'));
      expect(thirdImage).toBeTruthy();
      await deleteMedia(sellerApi, thirdImage!.id);
    });

    await test.step('Verify 2 images remain', async () => {
      gallery = await getGallery(sellerApi, 'Product', product.id);
      expect(gallery).toHaveLength(2);
      // Verify the deleted image is no longer present
      const deletedImage = gallery.find((img) => img.fileName.includes('product-3'));
      expect(deletedImage).toBeUndefined();
    });

    await test.step('Set second image as primary', async () => {
      const secondImage = gallery.find((img) => img.fileName.includes('product-2'));
      expect(secondImage).toBeTruthy();
      await setPrimaryMedia(sellerApi, 'Product', product.id, secondImage!.id);
    });

    await test.step('Verify primary changed', async () => {
      gallery = await getGallery(sellerApi, 'Product', product.id);
      const primaryImage = gallery.find((img) => img.isPrimary === true);
      expect(primaryImage).toBeTruthy();
      expect(primaryImage!.fileName).toContain('product-2');
    });
  });

  // ── Scenario 6: Validation — reject invalid SKU codes ──

  // TODO: Same Angular signal input issue as above.
  test.skip('should show validation errors for invalid SKUs in UI', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    const productForm = new ProductFormPage(page);

    await test.step('Ensure seller has a store', async () => {
      await page.goto('/seller');
      await page.waitForLoadState('domcontentloaded');

      const createStoreHeading = page.getByRole('heading', { name: 'Create Your Store' });
      const hasCreateForm = await createStoreHeading.isVisible({ timeout: TIMEOUTS.quick }).catch(() => false);

      if (hasCreateForm) {
        const storeNameInput = page.getByTestId('store-name-input');
        const storeDescInput = page.getByPlaceholder('Tell customers what your store is about...');
        const createStoreBtn = page.getByRole('button', { name: 'Create Store' });

        await expect(storeNameInput).toBeVisible({ timeout: TIMEOUTS.quick });

        await storeNameInput.click({ clickCount: 3 });
        await storeNameInput.pressSequentially('E2E Store ' + uniqueId, { delay: 20 });
        await storeDescInput.click({ clickCount: 3 });
        await storeDescInput.pressSequentially('Automated E2E test store for validation', { delay: 20 });
        await page.waitForTimeout(500);

        await createStoreBtn.click({ timeout: TIMEOUTS.element });
        await page.waitForTimeout(3000);
      }
    });

    await test.step('Navigate to product creation form', async () => {
      await page.goto('/seller/products/new');
      await page.waitForLoadState('domcontentloaded');
      await expect(productForm.pageHeading).toBeVisible({ timeout: TIMEOUTS.api });
    });

    await test.step('Fill product info', async () => {
      await productForm.fillProductInfo({
        name: `Validation Test Product ${uniqueId}`,
        description: 'Product for testing SKU validation',
        brand: 'ValTestBrand',
        category: 'Electronics',
        tags: 'validation',
      });
    });

    await test.step('Try to submit without SKUs — expect error', async () => {
      await productForm.submit();
      const errorToast = page.locator('[role="alert"]').filter({ hasText: /sku|required/i });
      await expect(errorToast).toBeVisible({ timeout: TIMEOUTS.quick });
    });

    await test.step('Add SKU with empty code — expect error', async () => {
      await productForm.addVariant();
      await productForm.fillSkuInfo(0, {
        skuCode: '',
        price: '29.99',
        currency: 'USD',
      });
      await productForm.submit();
      // Expect inline validation or toast
      const emptyCodeError = page.getByText(/sku code.*required|required.*sku code/i);
      await expect(emptyCodeError).toBeVisible({ timeout: TIMEOUTS.quick });
    });

    await test.step('Add valid first SKU, then duplicate code — expect error', async () => {
      // Fill the first SKU with a valid code
      await productForm.fillSkuInfo(0, {
        skuCode: `VALID-${uniqueId}`,
        price: '29.99',
        currency: 'USD',
      });

      // Add a second SKU with the same code
      await productForm.addVariant();
      await productForm.fillSkuInfo(1, {
        skuCode: `VALID-${uniqueId}`,
        price: '39.99',
        currency: 'USD',
      });

      await productForm.submit();
      // Expect duplicate error
      const duplicateError = page.getByText(/duplicate|already exists|unique/i);
      await expect(duplicateError).toBeVisible({ timeout: TIMEOUTS.quick });
    });

    await page.close();
  });
});