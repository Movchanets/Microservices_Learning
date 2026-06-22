import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { TIMEOUTS } from '../../utils/constants';
import {
  ensureCategoryExists,
  ensureProductExists,
} from '../../utils/catalog-helpers';
import { ensureStoreExists } from '../../utils/store-helpers';

test.describe('Home Page', () => {
  test.beforeAll(async ({ sellerApi, sellerUser, adminApi }) => {
    // Seed: store → category → product → SKU → activate → inventory
    const uniqueId = Math.random().toString(36).substring(7).toUpperCase();
    const store = await ensureStoreExists(
      sellerApi, adminApi, sellerUser.id,
      `Home Store ${uniqueId}`, 'E2E home-page test store'
    );
    const category = await ensureCategoryExists(adminApi, `Home Category ${uniqueId}`, 'Test category');
    await ensureProductExists(
      sellerApi,
      {
        name: `Home Product ${uniqueId}`,
        description: 'Product for home-page E2E test',
        categoryId: category.id,
        storeId: store.id,
        brand: 'TestBrand',
        tags: ['e2e', 'home'],
      },
      { skuCode: `HOME-SKU-${uniqueId}`, price: 79.99, currency: 'USD' },
      100
    );
  });

  test('should display Shop by Category heading and category tiles', async ({ page, homePage }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
      await page.reload();
    });

    await test.step('Verify Shop by Category heading', async () => {
      await expect(homePage.shopByCategoryHeading).toBeVisible({ timeout: TIMEOUTS.element });
    });

    await test.step('Verify category tiles are displayed', async () => {
      const tileCount = await homePage.getCategoryTileCount();
      expect(tileCount).toBeGreaterThan(0);
    });
  });

  test('should navigate to catalog when clicking a category tile', async ({ page, homePage }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
      await page.reload();
    });

    await test.step('Wait for category tiles to load', async () => {
      await expect(homePage.shopByCategoryHeading).toBeVisible({ timeout: TIMEOUTS.element });
    });

    await test.step('Click a category tile', async () => {
      await homePage.clickCategoryTile(0);
    });

    await test.step('Verify navigation to catalog', async () => {
      await expect(page).toHaveURL(/\/catalog/);
    });
  });

  test('should display featured products carousel', async ({ page, homePage }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
      await page.reload();
    });

    await test.step('Verify featured carousel is visible', async () => {
      await expect(homePage.featuredCarousel).toBeVisible({ timeout: TIMEOUTS.element });
    });

    await test.step('Verify featured products are displayed', async () => {
      const productCount = await homePage.getFeaturedProductCount();
      expect(productCount).toBeGreaterThan(0);
    });
  });

  test('should display new arrivals carousel', async ({ page, homePage }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
      await page.reload();
    });

    await test.step('Verify new arrivals carousel is visible', async () => {
      await expect(homePage.newArrivalsCarousel).toBeVisible({ timeout: TIMEOUTS.element });
    });
  });

  test('should navigate to product detail when clicking a featured product', async ({ page, homePage }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
      await page.reload();
    });

    await test.step('Wait for featured carousel', async () => {
      await expect(homePage.featuredCarousel).toBeVisible({ timeout: TIMEOUTS.element });
    });

    await test.step('Click first featured product', async () => {
      const firstProductLink = homePage.featuredCarousel.locator('app-product-card a').first();
      await firstProductLink.click();
    });

    await test.step('Verify navigation to product detail', async () => {
      await expect(page).toHaveURL(/\/catalog\/.+/);
    });
  });

  test('should have working header navigation on home page', async ({ page, homePage, header }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
      await page.reload();
    });

    await test.step('Verify header elements are visible', async () => {
      await expect(header.logo).toBeVisible();
      await expect(header.cartBtn).toBeVisible();
    });
  });
});
