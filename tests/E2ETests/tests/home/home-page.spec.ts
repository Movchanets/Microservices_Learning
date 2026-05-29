import { test, expect } from '../../fixtures/test-base';
import { TIMEOUTS } from '../../utils/constants';

test.describe('Home Page', () => {

  test('should display hero banner on load', async ({ page, homePage }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
    });

    await test.step('Verify hero banner is visible', async () => {
      await expect(homePage.heroBanner).toBeVisible();
    });
  });

  test('should display Shop by Category heading and category tiles', async ({ page, homePage }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
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

  test('should display deal of the day section when products exist', async ({ page, homePage }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
    });

    await test.step('Verify Deal of the Day section', async () => {
      await expect(homePage.dealOfTheDay).toBeVisible({ timeout: TIMEOUTS.element });
      await expect(homePage.dealOfTheDay.getByText('Deal of the Day')).toBeVisible();
    });
  });

  test('should display featured products carousel', async ({ page, homePage }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
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
    });

    await test.step('Verify new arrivals carousel is visible', async () => {
      await expect(homePage.newArrivalsCarousel).toBeVisible({ timeout: TIMEOUTS.element });
    });
  });

  test('should navigate to product detail when clicking a featured product', async ({ page, homePage }) => {
    await test.step('Navigate to home page', async () => {
      await homePage.goto();
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
    });

    await test.step('Verify header elements are visible', async () => {
      await expect(header.logo).toBeVisible();
      await expect(header.cartBtn).toBeVisible();
    });
  });
});
