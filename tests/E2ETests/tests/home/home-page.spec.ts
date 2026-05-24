import { test, expect } from '../../fixtures/test-base';

test.describe('Home Page', () => {

  test('should display hero banner and category tiles', async ({ page, homePage }) => {
    await homePage.goto();
    await expect(homePage.heroBanner).toBeVisible();
    await expect(homePage.shopByCategoryHeading).toBeVisible();
  });

  test('should display featured products carousel', async ({ page, homePage }) => {
    await homePage.goto();
    await expect(homePage.featuredCarousel).toBeVisible();
    const count = await homePage.getFeaturedProductCount();
    expect(count).toBeGreaterThan(0);
  });

  test('should navigate to catalog when clicking a category tile', async ({ page, homePage }) => {
    await homePage.goto();
    const tileCount = await homePage.getCategoryTileCount();
    if (tileCount === 0) {
      test.skip(true, 'No category tiles available');
      return;
    }
    await homePage.clickCategoryTile(0);
    await expect(page).toHaveURL(/\/catalog/);
  });

  test('should add product to cart from featured carousel', async ({ page, homePage, header }) => {
    await homePage.goto();
    const count = await homePage.getFeaturedProductCount();
    if (count === 0) {
      test.skip(true, 'No featured products available');
      return;
    }
    await homePage.addToCartFromCarousel(0);
    await page.waitForLoadState('domcontentloaded');
    const hasBadge = await header.hasCartBadge();
    expect(hasBadge).toBe(true);
  });

  test('should show deal of the day when featured products exist', async ({ page, homePage }) => {
    await homePage.goto();
    const count = await homePage.getFeaturedProductCount();
    if (count > 0) {
      await expect(homePage.dealOfTheDay).toBeVisible();
    }
  });

  test('should redirect unauthenticated user from home to home page', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/home/);
  });
});
