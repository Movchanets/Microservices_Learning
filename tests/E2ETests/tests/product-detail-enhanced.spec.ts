import { test, expect } from '../fixtures/test-base';
import { ensureAuthenticatedPageViaApi } from '../utils/api-helpers';
import { ProductDetailEnhancedPage } from '../pages/product-detail-enhanced.page';

test.describe('Product Detail Enhancements', () => {

  test('should display product detail with buy box', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const pdp = new ProductDetailEnhancedPage(page);

    await page.goto('/catalog');
    await page.waitForLoadState('domcontentloaded');

    const firstProduct = page.locator('[data-testid^="product-card-"] a').first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('domcontentloaded');
    await expect(pdp.addToCartBtn).toBeVisible();
    await context.close();
  });

  test('should show stock indicator on product detail', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const pdp = new ProductDetailEnhancedPage(page);

    await page.goto('/catalog');
    await page.waitForLoadState('domcontentloaded');

    const firstProduct = page.locator('[data-testid^="product-card-"] a').first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('domcontentloaded');
    const stockVisible = await pdp.stockIndicator.isVisible();
    expect(stockVisible).toBe(true);
    await context.close();
  });

  test('should change quantity with plus/minus buttons', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const pdp = new ProductDetailEnhancedPage(page);

    await page.goto('/catalog');
    await page.waitForLoadState('domcontentloaded');

    const firstProduct = page.locator('[data-testid^="product-card-"] a').first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('domcontentloaded');
    await pdp.increaseQuantity();
    await pdp.decreaseQuantity();
    await context.close();
  });

  test('should add product to cart from detail page', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const pdp = new ProductDetailEnhancedPage(page);

    await page.goto('/catalog');
    await page.waitForLoadState('domcontentloaded');

    const firstProduct = page.locator('[data-testid^="product-card-"] a').first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('domcontentloaded');
    await pdp.addToCart();
    await page.waitForLoadState('domcontentloaded');
    await context.close();
  });

  test('should display review section when reviews exist', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const pdp = new ProductDetailEnhancedPage(page);

    await page.goto('/catalog');
    await page.waitForLoadState('domcontentloaded');

    const firstProduct = page.locator('[data-testid^="product-card-"] a').first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('domcontentloaded');
    const hasReviews = await pdp.reviewSummary.isVisible();
    const hasNoReviews = await page.getByText(/no reviews|be the first/i).isVisible();
    expect(hasReviews || hasNoReviews).toBe(true);
    await context.close();
  });

  test('should show frequently bought together section', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const pdp = new ProductDetailEnhancedPage(page);

    await page.goto('/catalog');
    await page.waitForLoadState('domcontentloaded');

    const firstProduct = page.locator('[data-testid^="product-card-"] a').first();
    await expect(firstProduct).toBeVisible({ timeout: 10000 });
    await firstProduct.click();
    await page.waitForLoadState('domcontentloaded');
    const hasFBT = await pdp.frequentlyBoughtTogether.isVisible();
    expect(hasFBT).toBe(true);
    await context.close();
  });
});
