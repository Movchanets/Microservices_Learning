import { test, expect } from '../fixtures/test-base';
import { ensureAuthenticatedPageViaApi } from '../utils/api-helpers';

test.describe('Seller Orders Management', () => {

  test('should display seller orders tab', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request, { firstName: 'Seller', lastName: 'User' });
    await page.goto('/seller');
    await page.waitForLoadState('domcontentloaded');

    const ordersTab = page.getByRole('link', { name: /orders/i });
    await expect(ordersTab).toBeVisible({ timeout: 10000 });
    await ordersTab.click();
    await page.waitForLoadState('domcontentloaded');
    const hasTable = await page.locator('table').isVisible();
    const isEmpty = await page.getByText('No orders yet').isVisible();
    expect(hasTable || isEmpty).toBe(true);
    await context.close();
  });

  test('should show orders table with status', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request, { firstName: 'Seller', lastName: 'User' });
    await page.goto('/seller');
    await page.waitForLoadState('domcontentloaded');

    const ordersTab = page.getByRole('link', { name: /orders/i });
    await expect(ordersTab).toBeVisible({ timeout: 10000 });
    await ordersTab.click();
    await page.waitForLoadState('domcontentloaded');
    const hasTable = await page.locator('table').isVisible();
    const isEmpty = await page.getByText('No orders yet').isVisible();
    expect(hasTable || isEmpty).toBe(true);

    if (hasTable) {
      await expect(page.getByRole('columnheader', { name: /order id/i })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: /status/i })).toBeVisible();
    }
    await context.close();
  });

  test('should show status update buttons for seller', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request, { firstName: 'Seller', lastName: 'User' });
    await page.goto('/seller');
    await page.waitForLoadState('domcontentloaded');

    const ordersTab = page.getByRole('link', { name: /orders/i });
    await expect(ordersTab).toBeVisible({ timeout: 10000 });
    await ordersTab.click();
    await page.waitForLoadState('domcontentloaded');
    const updateBtns = page.getByRole('button', { name: /mark|update/i });
    const count = await updateBtns.count();
    expect(count).toBeGreaterThan(0);
    await context.close();
  });

  test('should redirect unauthenticated from seller orders', async ({ page }) => {
    await page.context().clearCookies();
    await page.evaluate(() => localStorage.clear());
    await page.goto('/seller/orders');
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/auth\/login/);
  });
});
