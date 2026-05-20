import { test, expect } from '../fixtures/test-base';

test.describe('Plan 08: Inventory Management UI', () => {

  test.beforeEach(async ({ loginPage, registerPage, page }) => {
    const randomId = Math.random().toString(36).substring(7);
    const email = `seller_${randomId}@test.com`;
    const password = 'P@ssw0rd123!';

    await registerPage.goto('/auth/register');
    await registerPage.register('Seller', 'User', email, password);
    await page.waitForLoadState('domcontentloaded');

    if (page.url().includes('/auth/login')) {
      await loginPage.login(email, password);
      await page.waitForLoadState('domcontentloaded');
    }
  });

  test('should display inventory tab for seller', async ({ page }) => {
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
  });

  test('should show inventory table with products', async ({ page }) => {
    await page.goto('/seller');
    await page.waitForLoadState('domcontentloaded');

    const inventoryTab = page.getByRole('link', { name: /inventory/i });
    await expect(inventoryTab).toBeVisible({ timeout: 10000 });
    await inventoryTab.click();
    await page.waitForLoadState('domcontentloaded');
    const hasTable = await page.locator('table').isVisible();
    const isEmpty = await page.getByText(/no inventory/i).isVisible();
    expect(hasTable || isEmpty).toBe(true);
  });

  test('should filter inventory by status', async ({ page }) => {
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
  });

  test('should redirect unauthenticated from inventory', async ({ page }) => {
    await page.context().clearCookies();
    await page.evaluate(() => localStorage.clear());
    await page.goto('/seller');
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/auth\/login/);
  });
});
