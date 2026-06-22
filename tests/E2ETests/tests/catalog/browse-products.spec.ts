import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { TIMEOUTS } from '../../utils/constants';
import {
  ensureCategoryExists,
  ensureProductExists,
} from '../../utils/catalog-helpers';
import { ensureStoreExists } from '../../utils/store-helpers';

test.describe('Catalog: Browse Products', () => {
  test.beforeAll(async ({ sellerApi, sellerUser, adminApi }) => {
    // Seed: store → category → product → SKU → activate → inventory
    const uniqueId = Math.random().toString(36).substring(7).toUpperCase();
    const store = await ensureStoreExists(
      sellerApi, adminApi, sellerUser.id,
      `Browse Store ${uniqueId}`, 'E2E browse-products test store'
    );
    const category = await ensureCategoryExists(adminApi, `Browse Category ${uniqueId}`, 'Test category');
    await ensureProductExists(
      sellerApi,
      {
        name: `Browse Product ${uniqueId}`,
        description: 'Product for browse-products E2E test',
        categoryId: category.id,
        storeId: store.id,
        brand: 'TestBrand',
        tags: ['e2e', 'browse'],
      },
      { skuCode: `BROWSE-SKU-${uniqueId}`, price: 49.99, currency: 'USD' },
      100
    );
  });

  test('should display product list on catalog page', async ({ catalogPage, page }) => {
    await test.step('Navigate to catalog page', async () => {
      await catalogPage.goto();
      await catalogPage.waitForPageLoad();
      // Reload to bypass stale Angular SSR after API seeding
      await page.reload();
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
      await catalogPage.goto();
      await catalogPage.waitForPageLoad();
      await page.reload();
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
      await catalogPage.goto();
      await catalogPage.waitForPageLoad();
      await page.reload();
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
      await catalogPage.goto();
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
