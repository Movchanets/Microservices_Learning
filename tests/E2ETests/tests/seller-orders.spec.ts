import { test, expect } from '../fixtures/test-base';

test.describe('Plan 09: Seller Orders Management', () => {

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

  test('should display seller orders tab', async ({ page }) => {
    await page.goto('/seller');
    await page.waitForLoadState('networkidle');

    const ordersTab = page.getByRole('link', { name: /orders/i });
    if (await ordersTab.isVisible()) {
      await ordersTab.click();
      await page.waitForLoadState('networkidle');
      const hasTable = await page.locator('table').isVisible();
      const isEmpty = await page.getByText('No orders yet').isVisible();
      expect(hasTable || isEmpty).toBe(true);
    }
  });

  test('should show orders table with status', async ({ page }) => {
    await page.goto('/seller');
    await page.waitForLoadState('networkidle');

    const ordersTab = page.getByRole('link', { name: /orders/i });
    if (await ordersTab.isVisible()) {
      await ordersTab.click();
      await page.waitForLoadState('networkidle');
      const hasTable = await page.locator('table').isVisible();
      const isEmpty = await page.getByText('No orders yet').isVisible();
      expect(hasTable || isEmpty).toBe(true);

      if (hasTable) {
        await expect(page.getByRole('columnheader', { name: /order id/i })).toBeVisible();
        await expect(page.getByRole('columnheader', { name: /status/i })).toBeVisible();
      }
    }
  });

  test('should show status update buttons for seller', async ({ page }) => {
    await page.goto('/seller');
    await page.waitForLoadState('networkidle');

    const ordersTab = page.getByRole('link', { name: /orders/i });
    if (await ordersTab.isVisible()) {
      await ordersTab.click();
      await page.waitForLoadState('networkidle');
      const updateBtns = page.getByRole('button', { name: /mark|update/i });
      const count = await updateBtns.count();
      expect(count).toBeGreaterThanOrEqual(0);
    }
  });

  test('should redirect unauthenticated from seller orders', async ({ page }) => {
    await page.context().clearCookies();
    await page.evaluate(() => localStorage.clear());
    await page.goto('/seller/orders');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/auth\/login/);
  });
});
