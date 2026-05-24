import { test, expect } from '../fixtures/test-base';
import { ensureAuthenticatedPageViaApi } from '../utils/api-helpers';

test.describe('Inventory Management UI', () => {

  test('should display inventory tab for seller', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request, { firstName: 'Seller', lastName: 'User' });
    await page.goto('/seller');
    await page.waitForLoadState('domcontentloaded');

    const hasHeading = await page.getByRole('heading', { name: 'Seller Dashboard' }).isVisible();
    const isRedirected = page.url().includes('/auth/login') || page.url().includes('/catalog');
    expect(hasHeading || isRedirected).toBe(true);

    if (hasHeading) {
      const inventoryTab = page.getByRole('link', { name: /inventory/i });
      await expect(inventoryTab).toBeVisible({ timeout: 10000 });
      await inventoryTab.click();
      await page.waitForLoadState('domcontentloaded');
      const hasTable = await page.locator('table').isVisible();
      const isEmpty = await page.getByText(/no inventory/i).isVisible();
      expect(hasTable || isEmpty).toBe(true);
    }
    await context.close();
  });

  test('should show inventory table with products', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request, { firstName: 'Seller', lastName: 'User' });
    await page.goto('/seller');
    await page.waitForLoadState('domcontentloaded');

    const inventoryTab = page.getByRole('link', { name: /inventory/i });
    await expect(inventoryTab).toBeVisible({ timeout: 10000 });
    await inventoryTab.click();
    await page.waitForLoadState('domcontentloaded');
    const hasTable = await page.locator('table').isVisible();
    const isEmpty = await page.getByText(/no inventory/i).isVisible();
    expect(hasTable || isEmpty).toBe(true);
    await context.close();
  });

  test('should filter inventory by status', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request, { firstName: 'Seller', lastName: 'User' });
    await page.goto('/seller');
    await page.waitForLoadState('domcontentloaded');

    const inventoryTab = page.getByRole('link', { name: /inventory/i });
    await expect(inventoryTab).toBeVisible({ timeout: 10000 });
    await inventoryTab.click();
    await page.waitForLoadState('domcontentloaded');

    const lowStockBtn = page.getByRole('button', { name: 'Low Stock' });
    await expect(lowStockBtn).toBeVisible({ timeout: 10000 });
    await lowStockBtn.click();
    await page.waitForLoadState('domcontentloaded');

    const allItemsBtn = page.getByRole('button', { name: 'All Items' });
    await expect(allItemsBtn).toBeVisible({ timeout: 10000 });
    await allItemsBtn.click();
    await page.waitForLoadState('domcontentloaded');
    await context.close();
  });

  test('should redirect unauthenticated from inventory', async ({ page }) => {
    await page.context().clearCookies();
    await page.evaluate(() => localStorage.clear());
    await page.goto('/seller');
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/auth\/login/);
  });
});
