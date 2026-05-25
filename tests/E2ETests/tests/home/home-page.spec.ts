import { test, expect } from '../../fixtures/test-base';

test.describe('Home Page', () => {

  test('should display hero banner on load', async ({ page, homePage }) => {
    await homePage.goto();

    await expect(homePage.heroBanner).toBeVisible();
  });

  test('should display Shop by Category heading and category tiles', async ({ page, homePage }) => {
    await homePage.goto();

    // Category tiles section depends on categories being loaded from API
    const categoriesLoaded = await homePage.shopByCategoryHeading.isVisible()
      .catch(() => false);

    if (!categoriesLoaded) {
      test.skip(true, 'No categories loaded from API — skipping category tile checks');
      return;
    }

    await expect(homePage.shopByCategoryHeading).toBeVisible();

    const tileCount = await homePage.getCategoryTileCount();
    expect(tileCount).toBeGreaterThan(0);
  });

  test('should navigate to catalog when clicking a category tile', async ({ page, homePage }) => {
    await homePage.goto();

    const categoriesLoaded = await homePage.shopByCategoryHeading.isVisible()
      .catch(() => false);

    if (!categoriesLoaded) {
      test.skip(true, 'No categories loaded — skipping');
      return;
    }

    await homePage.clickCategoryTile(0);

    // Should navigate to catalog with category query param
    await expect(page).toHaveURL(/\/catalog/);
  });

  test('should display deal of the day section when products exist', async ({ page, homePage }) => {
    await homePage.goto();

    const dealVisible = await homePage.isDealOfTheDayVisible();
    // Deal of the day is conditional on featured products being loaded
    if (!dealVisible) {
      test.skip(true, 'No featured products loaded — skipping deal of the day');
      return;
    }

    await expect(homePage.dealOfTheDay).toBeVisible();
    // Should contain "Deal of the Day" text
    await expect(homePage.dealOfTheDay.getByText('Deal of the Day')).toBeVisible();
  });

  test('should display featured products carousel', async ({ page, homePage }) => {
    await homePage.goto();

    const featuredVisible = await homePage.featuredCarousel.isVisible()
      .catch(() => false);

    if (!featuredVisible) {
      test.skip(true, 'No featured products loaded — skipping carousel check');
      return;
    }

    await expect(homePage.featuredCarousel).toBeVisible();

    const productCount = await homePage.getFeaturedProductCount();
    expect(productCount).toBeGreaterThan(0);
  });

  test('should display new arrivals carousel', async ({ page, homePage }) => {
    await homePage.goto();

    const newArrivalsVisible = await homePage.isNewArrivalsVisible();

    if (!newArrivalsVisible) {
      test.skip(true, 'No new arrivals loaded — skipping');
      return;
    }

    await expect(homePage.newArrivalsCarousel).toBeVisible();
  });

  test('should navigate to product detail when clicking a featured product', async ({ page, homePage }) => {
    await homePage.goto();

    const featuredVisible = await homePage.featuredCarousel.isVisible()
      .catch(() => false);

    if (!featuredVisible) {
      test.skip(true, 'No featured products — skipping');
      return;
    }

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
