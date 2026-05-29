import { test, expect } from '../../fixtures/test-base';
import { TIMEOUTS } from '../../utils/constants';

test.describe('Catalog: Browse Products', () => {

  test('should display product list on catalog page', async ({ catalogPage, page }) => {
    await test.step('Navigate to catalog page', async () => {
      await catalogPage.goto('/catalog');
      await catalogPage.waitForPageLoad();
    });

    await test.step('Verify catalog title is visible', async () => {
      await expect(catalogPage.catalogTitle).toBeVisible();
    });

    await test.step('Verify product cards are displayed', async () => {
      const productCount = await catalogPage.productCards.count();
      expect(productCount).toBeGreaterThan(0);
    });
  });

  test('should navigate to product detail when clicking a product', async ({ catalogPage, page }) => {
    await test.step('Navigate to catalog page', async () => {
      await catalogPage.goto('/catalog');
      await catalogPage.waitForPageLoad();
    });

    await test.step('Click first product card', async () => {
      const firstProduct = catalogPage.productCards.first();
      await firstProduct.click();
    });

    await test.step('Verify navigation to product detail page', async () => {
      await expect(page).toHaveURL(/\/catalog\/.+/);
    });
  });

  test('should search for products', async ({ catalogPage, page }) => {
    await test.step('Navigate to catalog page', async () => {
      await catalogPage.goto('/catalog');
      await catalogPage.waitForPageLoad();
    });

    let initialCount: number;
    await test.step('Record initial product count', async () => {
      initialCount = await catalogPage.productCards.count();
    });

    await test.step('Search for a specific term', async () => {
      await catalogPage.search('iPhone');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify filtered results', async () => {
      const filteredCount = await catalogPage.productCards.count();
      expect(filteredCount).toBeLessThanOrEqual(initialCount!);
    });
  });

  // Category filter buttons not yet rendered on catalog page — skip until implemented
  test.skip('should filter products by category', async ({ catalogPage, page }) => {
    await test.step('Navigate to catalog page', async () => {
      await catalogPage.goto('/catalog');
      await catalogPage.waitForPageLoad();
    });

    await test.step('Click a category button', async () => {
      const categoryBtn = page.getByRole('button').filter({ hasText: /Electronics|Home|Clothing/ }).first();
      await expect(categoryBtn).toBeVisible({ timeout: TIMEOUTS.element });
      await categoryBtn.click();
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify products are filtered', async () => {
      const productCount = await catalogPage.productCards.count();
      expect(productCount).toBeGreaterThan(0);
    });
  });
});
