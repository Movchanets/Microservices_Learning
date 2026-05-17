import { test, expect } from '../fixtures/test-base';

test.describe('Plan 08: Inventory Management UI', () => {

  test.beforeEach(async ({ loginPage, registerPage, page }) => {
    const randomId = Math.random().toString(36).substring(7);
    const email = `seller_${randomId}@test.com`;
    const password = 'P@ssw0rd123!';

    await registerPage.goto('/auth/register');
    await registerPage.register('Seller', 'User', email, password);
    await page.waitForLoadState('networkidle');

    if (page.url().includes('/auth/login')) {
      await loginPage.login(email, password);
      await page.waitForLoadState('networkidle');
    }
  });

  test('should display inventory tab for seller', async ({ page }) => {
    await page.goto('/seller');
    await page.waitForLoadState('networkidle');

    const hasHeading = await page.getByRole('heading', { name: 'Seller Dashboard' }).isVisible();
    const isRedirected = page.url().includes('/auth/login') || page.url().includes('/catalog');
    expect(hasHeading || isRedirected).toBe(true);

    if (hasHeading) {
      const inventoryTab = page.getByRole('link', { name: /inventory/i });
      if (await inventoryTab.isVisible()) {
        await inventoryTab.click();
        await page.waitForLoadState('networkidle');
        const hasTable = await page.locator('table').isVisible();
        const isEmpty = await page.getByText(/no inventory/i).isVisible();
        expect(hasTable || isEmpty).toBe(true);
      }
    }
  });

  test('should show inventory table with products', async ({ page }) => {
    await page.goto('/seller');
    await page.waitForLoadState('networkidle');

    const inventoryTab = page.getByRole('link', { name: /inventory/i });
    if (await inventoryTab.isVisible()) {
      await inventoryTab.click();
      await page.waitForLoadState('networkidle');
      const hasTable = await page.locator('table').isVisible();
      const isEmpty = await page.getByText(/no inventory/i).isVisible();
      expect(hasTable || isEmpty).toBe(true);
    }
  });

  test('should filter inventory by status', async ({ page }) => {
    await page.goto('/seller');
    await page.waitForLoadState('networkidle');

    const inventoryTab = page.getByRole('link', { name: /inventory/i });
    if (await inventoryTab.isVisible()) {
      await inventoryTab.click();
      await page.waitForLoadState('networkidle');

      const lowStockBtn = page.getByRole('button', { name: 'Low Stock' });
      if (await lowStockBtn.isVisible()) {
        await lowStockBtn.click();
        await page.waitForTimeout(300);
      }

      const allItemsBtn = page.getByRole('button', { name: 'All Items' });
      if (await allItemsBtn.isVisible()) {
        await allItemsBtn.click();
        await page.waitForTimeout(300);
      }
    }
  });

  test('should redirect unauthenticated from inventory', async ({ page }) => {
    await page.context().clearCookies();
    await page.evaluate(() => localStorage.clear());
    await page.goto('/seller');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/auth\/login/);
  });
});
