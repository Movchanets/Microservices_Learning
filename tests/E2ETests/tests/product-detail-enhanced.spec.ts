import { test, expect } from '../fixtures/test-base';

test.describe('Plan 04: Product Detail Enhancements', () => {

  test.beforeEach(async ({ loginPage, registerPage, page }) => {
    const randomId = Math.random().toString(36).substring(7);
    const email = `user_${randomId}@test.com`;
    const password = 'P@ssw0rd123!';

    await registerPage.goto('/auth/register');
    await registerPage.register('Test', 'User', email, password);
    await page.waitForLoadState('networkidle');

    if (page.url().includes('/auth/login')) {
      await loginPage.login(email, password);
      await expect(page).toHaveURL(/\/catalog/);
    }
  });

  test('should display product detail with buy box', async ({ page, productDetailEnhancedPage }) => {
    await page.goto('/catalog');
    await page.waitForLoadState('networkidle');

    const firstProduct = page.locator('[data-testid^="product-card-"] a').first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('networkidle');
    await expect(productDetailEnhancedPage.addToCartBtn).toBeVisible();
  });

  test('should show stock indicator on product detail', async ({ page, productDetailEnhancedPage }) => {
    await page.goto('/catalog');
    await page.waitForLoadState('networkidle');

    const firstProduct = page.locator('[data-testid^="product-card-"] a').first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('networkidle');
    const stockVisible = await productDetailEnhancedPage.stockIndicator.isVisible();
    expect(stockVisible).toBe(true);
  });

  test('should change quantity with plus/minus buttons', async ({ page, productDetailEnhancedPage }) => {
    await page.goto('/catalog');
    await page.waitForLoadState('networkidle');

    const firstProduct = page.locator('[data-testid^="product-card-"] a').first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('networkidle');
    await productDetailEnhancedPage.increaseQuantity();
    await productDetailEnhancedPage.decreaseQuantity();
  });

  test('should add product to cart from detail page', async ({ page, productDetailEnhancedPage }) => {
    await page.goto('/catalog');
    await page.waitForLoadState('networkidle');

    const firstProduct = page.locator('[data-testid^="product-card-"] a').first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('networkidle');
    await productDetailEnhancedPage.addToCart();
    await page.waitForLoadState('networkidle');
  });

  test('should display review section when reviews exist', async ({ page, productDetailEnhancedPage }) => {
    await page.goto('/catalog');
    await page.waitForLoadState('networkidle');

    const firstProduct = page.locator('[data-testid^="product-card-"] a').first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('networkidle');
    const hasReviews = await productDetailEnhancedPage.reviewSummary.isVisible();
    const hasNoReviews = await page.getByText(/no reviews|be the first/i).isVisible();
    expect(hasReviews || hasNoReviews).toBe(true);
  });

  test('should show frequently bought together section', async ({ page, productDetailEnhancedPage }) => {
    await page.goto('/catalog');
    await page.waitForLoadState('networkidle');

    const firstProduct = page.locator('[data-testid^="product-card-"] a').first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('networkidle');
    const hasFBT = await productDetailEnhancedPage.frequentlyBoughtTogether.isVisible();
    expect(hasFBT).toBe(true); // Frequently bought together should be visible
  });
});
