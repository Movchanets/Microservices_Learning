import { test, expect } from '../../fixtures/test-base';

test.describe('Home Page', () => {

  test('should display hero banner on load', async ({ page, homePage }) => {
    await homePage.goto();

    await expect(homePage.heroBanner).toBeVisible();
  });

  test('should display Shop by Category heading and category tiles', async ({ page, homePage }) => {
    await homePage.goto();

    await expect(homePage.shopByCategoryHeading).toBeVisible({ timeout: 10_000 });

    const tileCount = await homePage.getCategoryTileCount();
    expect(tileCount).toBeGreaterThan(0);
  });

  test('should navigate to catalog when clicking a category tile', async ({ page, homePage }) => {
    await homePage.goto();

    await expect(homePage.shopByCategoryHeading).toBeVisible({ timeout: 10_000 });

    await homePage.clickCategoryTile(0);

    // Should navigate to catalog with category query param
    await expect(page).toHaveURL(/\/catalog/);
  });

  test('should display deal of the day section when products exist', async ({ page, homePage }) => {
    await homePage.goto();

    await expect(homePage.dealOfTheDay).toBeVisible({ timeout: 10_000 });
    await expect(homePage.dealOfTheDay.getByText('Deal of the Day')).toBeVisible();
  });

  test('should display featured products carousel', async ({ page, homePage }) => {
    await homePage.goto();

    await expect(homePage.featuredCarousel).toBeVisible({ timeout: 10_000 });

    const productCount = await homePage.getFeaturedProductCount();
    expect(productCount).toBeGreaterThan(0);
  });

  test('should display new arrivals carousel', async ({ page, homePage }) => {
    await homePage.goto();

    await expect(homePage.newArrivalsCarousel).toBeVisible({ timeout: 10_000 });
  });

  test('should navigate to product detail when clicking a featured product', async ({ page, homePage }) => {
    await homePage.goto();

    await expect(homePage.featuredCarousel).toBeVisible({ timeout: 10_000 });

    // Click the first product card link in the featured carousel
    const firstProductLink = homePage.featuredCarousel.locator('app-product-card a').first();
    await firstProductLink.click();

    await expect(page).toHaveURL(/\/catalog\/.+/);
  });

  test('should have working header navigation on home page', async ({ page, homePage, header }) => {
    await homePage.goto();

    // Header should be visible with logo
    await expect(header.logo).toBeVisible();
    await expect(header.cartBtn).toBeVisible();
  });
});
