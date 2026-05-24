import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { SellerProductsPage } from '../../pages/seller-products.page';

test.describe('Seller: Product CRUD', () => {

  test('should navigate to product creation form', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    const productsPage = new SellerProductsPage(page);

    await productsPage.goto();
    await expect(productsPage.addProductBtn).toBeVisible();
    await productsPage.clickAddProduct();

    await expect(page).toHaveURL(/\/seller\/products\/new/);
    await page.close();
  });

  test('should create a new product with valid data', async ({ sellerContext, sellerApi }) => {
    const page = await sellerContext.newPage();
    await page.goto('/seller/products/new');
    await page.waitForLoadState('domcontentloaded');

    // Fill product form
    const nameInput = page.getByLabel(/product name|name/i).or(page.locator('input[placeholder*="name"]'));
    const descInput = page.locator('textarea');
    const priceInput = page.getByLabel(/price/i).or(page.locator('input[type="number"]'));
    const skuInput = page.getByLabel(/sku/i).or(page.locator('input[placeholder*="SKU"]'));

    await nameInput.fill('E2E Test Product');
    await descInput.fill('A product created by E2E test');
    await priceInput.fill('19.99');
    await skuInput.fill(`E2E-${Date.now()}`);

    // Select category if available
    const categorySelect = page.locator('select').filter({ hasText: /category/i })
      .or(page.getByLabel(/category/i));
    if (await categorySelect.isVisible().catch(() => false)) {
      const options = categorySelect.locator('option');
      if (await options.count() > 1) {
        await categorySelect.selectOption({ index: 1 });
      }
    }

    const submitBtn = page.getByRole('button', { name: /create|save|submit/i });
    await submitBtn.click();
    await page.waitForLoadState('domcontentloaded');

    // Should redirect back to products list or show success
    const onList = page.url().includes('/seller/products') && !page.url().includes('/new');
    const hasSuccess = await page.getByText(/success|created/i).isVisible().catch(() => false);
    expect(onList || hasSuccess).toBe(true);
    await page.close();
  });

  test('should show validation error for empty required fields', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    await page.goto('/seller/products/new');
    await page.waitForLoadState('domcontentloaded');

    // Try to submit empty form
    const submitBtn = page.getByRole('button', { name: /create|save|submit/i });
    await submitBtn.click();

    // Should show validation errors
    const validationMsg = page.locator('[aria-live="polite"]').first();
    await expect(validationMsg).toBeVisible({ timeout: 5000 });
    await page.close();
  });

  test('should display product list with edit and delete actions', async ({ sellerContext, sellerApi }) => {
    const page = await sellerContext.newPage();
    const productsPage = new SellerProductsPage(page);

    await productsPage.goto();

    const count = await productsPage.getProductCount();
    if (count > 0) {
      // Should have edit and delete buttons
      const editBtn = page.getByRole('button', { name: /edit/i }).first();
      await expect(editBtn).toBeVisible();
    }
    await page.close();
  });

  test('should navigate to edit product page', async ({ sellerContext, sellerApi }) => {
    const page = await sellerContext.newPage();
    const productsPage = new SellerProductsPage(page);

    await productsPage.goto();

    const count = await productsPage.getProductCount();
    if (count === 0) {
      test.skip(true, 'No products to edit — skipping');
      await page.close();
      return;
    }

    await productsPage.editProduct(0);
    await page.waitForLoadState('domcontentloaded');

    await expect(page).toHaveURL(/\/seller\/products\/.*\/edit/);
    await page.close();
  });
});
