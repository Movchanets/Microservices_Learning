import { test, expect } from '../fixtures/test-base';
import { ensureAuthenticatedPageViaApi } from '../utils/api-helpers';
import { MegaMenuComponent } from '../components/mega-menu.component';
import { HeaderComponent } from '../components/header.component';
import { CartDrawerComponent } from '../components/cart-drawer.component';

test.describe('Header & Mega-Menu', () => {

  test('should display mega menu when clicking Catalog button', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const header = new HeaderComponent(page);

    await header.toggleMegaMenu();
    await expect(header.megaMenu).toBeVisible();
    await context.close();
  });

  test('should close mega menu when clicking a category', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const header = new HeaderComponent(page);
    const megaMenu = new MegaMenuComponent(page);

    await header.toggleMegaMenu();
    await expect(header.megaMenu).toBeVisible();

    const rootCategories = await megaMenu.getRootCategoryNames();
    if (rootCategories.length > 0) {
      await megaMenu.clickCategory(rootCategories[0]);
      await page.waitForLoadState('domcontentloaded');
      await expect(page).toHaveURL(/\/catalog/);
    }
    await context.close();
  });

  test('should show subcategories on root category hover', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const header = new HeaderComponent(page);
    const megaMenu = new MegaMenuComponent(page);

    await header.toggleMegaMenu();

    const rootCategories = await megaMenu.getRootCategoryNames();
    if (rootCategories.length > 0) {
      await megaMenu.hoverRootCategory(rootCategories[0]);
      const subcats = await megaMenu.getVisibleSubcategories();
      expect(subcats.length).toBeGreaterThan(0);
    }
    await context.close();
  });

  test('should search products from header search bar', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const header = new HeaderComponent(page);

    await header.search('laptop');
    await expect(page).toHaveURL(/\/catalog.*q=laptop/);
    await context.close();
  });

  test('should show cart badge when items in cart', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const header = new HeaderComponent(page);

    const addBtn = page.getByRole('button', { name: /add to cart/i }).first();
    await expect(addBtn).toBeVisible({ timeout: 10000 });
    await addBtn.click();
    await page.waitForLoadState('domcontentloaded');
    const hasBadge = await header.hasCartBadge();
    expect(hasBadge).toBe(true);
    await context.close();
  });

  test('should open cart drawer when clicking cart icon', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const header = new HeaderComponent(page);
    const cartDrawer = new CartDrawerComponent(page);

    await header.openCart();
    await cartDrawer.waitForOpen();
    await context.close();
  });
});
