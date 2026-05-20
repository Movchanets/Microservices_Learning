import { test, expect } from '../fixtures/test-base';

test.describe('Plan 01: Header & Mega-Menu', () => {

  test.beforeEach(async ({ loginPage, registerPage, page }) => {
    // Register a fresh user for each test
    const randomId = Math.random().toString(36).substring(7);
    const email = `user_${randomId}@test.com`;
    const password = 'P@ssw0rd123!';

    await registerPage.goto('/auth/register');
    await registerPage.register('Test', 'User', email, password);
    await page.waitForLoadState('domcontentloaded');

    // If redirected to login, login
    if (page.url().includes('/auth/login')) {
      await loginPage.login(email, password);
      await expect(page).toHaveURL(/\/catalog/);
    }
  });

  test('should display mega menu when clicking Catalog button', async ({ page, header }) => {
    await header.toggleMegaMenu();
    await expect(header.megaMenu).toBeVisible();
  });

  test('should close mega menu when clicking a category', async ({ page, header, megaMenu }) => {
    await header.toggleMegaMenu();
    await expect(header.megaMenu).toBeVisible();

    const rootCategories = await megaMenu.getRootCategoryNames();
    if (rootCategories.length > 0) {
      await megaMenu.clickCategory(rootCategories[0]);
      await page.waitForLoadState('domcontentloaded');
      await expect(page).toHaveURL(/\/catalog/);
    }
  });

  test('should show subcategories on root category hover', async ({ page, header, megaMenu }) => {
    await header.toggleMegaMenu();

    const rootCategories = await megaMenu.getRootCategoryNames();
    if (rootCategories.length > 0) {
      await megaMenu.hoverRootCategory(rootCategories[0]);
      const subcats = await megaMenu.getVisibleSubcategories();
      expect(subcats.length).toBeGreaterThan(0);
    }
  });

  test('should search products from header search bar', async ({ page, header }) => {
    await header.search('laptop');
    await expect(page).toHaveURL(/\/catalog.*q=laptop/);
  });

  test('should show cart badge when items in cart', async ({ page, header }) => {
    const addBtn = page.getByRole('button', { name: /add to cart/i }).first();
    await expect(addBtn).toBeVisible({ timeout: 10000 });
    await addBtn.click();
    await page.waitForLoadState('domcontentloaded');
    const hasBadge = await header.hasCartBadge();
    expect(hasBadge).toBe(true);
  });

  test('should open cart drawer when clicking cart icon', async ({ page, header, cartDrawer }) => {
    await header.openCart();
    await cartDrawer.waitForOpen();
  });
});
