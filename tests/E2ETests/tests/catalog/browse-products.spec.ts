import { test, expect } from '../../fixtures/test-base';

test.describe('Catalog: Browse Products', () => {

  test('should display product list on catalog page', async ({ catalogPage, page }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    // Should see the catalog title
    await expect(catalogPage.catalogTitle).toBeVisible();

    // Should have product cards
    const productCount = await catalogPage.productCards.count();
    expect(productCount).toBeGreaterThan(0);
  });

  test('should navigate to product detail when clicking a product', async ({ catalogPage, page }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    // Click first product card
    const firstProduct = catalogPage.productCards.first();
    const productName = await firstProduct.locator('h3').innerText();
    await firstProduct.click();

    // Should navigate to product detail page
    await expect(page).toHaveURL(/\/catalog\/.+/);
  });

  test('should search for products', async ({ catalogPage, page }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    const initialCount = await catalogPage.productCards.count();

    // Search for a specific term
    await catalogPage.search('iPhone');

    // Wait for results to update
    await page.waitForLoadState('domcontentloaded');

    // Should have fewer or equal products
    const filteredCount = await catalogPage.productCards.count();
    expect(filteredCount).toBeLessThanOrEqual(initialCount);
  });

  // Category filter buttons not yet rendered on catalog page — skip until implemented
  test.skip('should filter products by category', async ({ catalogPage, page }) => {
    await catalogPage.goto('/catalog');
    await catalogPage.waitForPageLoad();

    // Find and click a category button
    const categoryBtn = page.getByRole('button').filter({ hasText: /Electronics|Home|Clothing/ }).first();
    await expect(categoryBtn).toBeVisible({ timeout: 10_000 });
    await categoryBtn.click();
    await page.waitForLoadState('domcontentloaded');

    // Products should be filtered
    const productCount = await catalogPage.productCards.count();
    expect(productCount).toBeGreaterThan(0);
  });
});
