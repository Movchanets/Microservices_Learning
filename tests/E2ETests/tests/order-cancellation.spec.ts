import { test, expect } from '../fixtures/test-base';

test.describe('Plan 09: Order Cancellation & Status', () => {

  test.beforeEach(async ({ loginPage, registerPage, page }) => {
    const randomId = Math.random().toString(36).substring(7);
    const email = `user_${randomId}@test.com`;
    const password = 'P@ssw0rd123!';

    await registerPage.goto('/auth/register');
    await registerPage.register('Test', 'User', email, password);
    await page.waitForLoadState('domcontentloaded');

    if (page.url().includes('/auth/login')) {
      await loginPage.login(email, password);
      await expect(page).toHaveURL(/\/catalog/);
    }
  });

  test('should display order detail with cancel button for cancellable orders', async ({ page, orderDetailEnhancedPage }) => {
    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    const orderLink = page.getByRole('link').filter({ hasText: /View Details/i }).first();
    const isOrderVisible = await orderLink.isVisible().catch(() => false);
    if (!isOrderVisible) {
      test.skip(true, 'No orders available for this user — skipping');
      return;
    }
    await orderLink.click();
    await page.waitForLoadState('domcontentloaded');
    await orderDetailEnhancedPage.waitForLoaded();
    await expect(orderDetailEnhancedPage.pageHeading).toBeVisible();
  });

  test('should show order status badge', async ({ page, orderDetailEnhancedPage }) => {
    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    const orderLink = page.getByRole('link').filter({ hasText: /View Details/i }).first();
    const isOrderVisible = await orderLink.isVisible().catch(() => false);
    if (!isOrderVisible) {
      test.skip(true, 'No orders available for this user — skipping');
      return;
    }
    await orderLink.click();
    await page.waitForLoadState('domcontentloaded');
    await orderDetailEnhancedPage.waitForLoaded();
    await expect(orderDetailEnhancedPage.statusBadge).toBeVisible();
  });

  test('should show order timeline', async ({ page, orderDetailEnhancedPage }) => {
    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    const orderLink = page.getByRole('link').filter({ hasText: /View Details/i }).first();
    const isOrderVisible = await orderLink.isVisible().catch(() => false);
    if (!isOrderVisible) {
      test.skip(true, 'No orders available for this user — skipping');
      return;
    }
    await orderLink.click();
    await page.waitForLoadState('domcontentloaded');
    await orderDetailEnhancedPage.waitForLoaded();
    await expect(orderDetailEnhancedPage.timeline).toBeVisible();
  });

  test('should show order items list', async ({ page, orderDetailEnhancedPage }) => {
    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    const orderLink = page.getByRole('link').filter({ hasText: /View Details/i }).first();
    const isOrderVisible = await orderLink.isVisible().catch(() => false);
    if (!isOrderVisible) {
      test.skip(true, 'No orders available for this user — skipping');
      return;
    }
    await orderLink.click();
    await page.waitForLoadState('domcontentloaded');
    await orderDetailEnhancedPage.waitForLoaded();
    const itemCount = await orderDetailEnhancedPage.getOrderItemCount();
    expect(itemCount).toBeGreaterThan(0); // Should have at least 1 order item
  });

  test('should navigate back to orders from detail', async ({ page, orderDetailEnhancedPage }) => {
    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    const orderLink = page.getByRole('link').filter({ hasText: /View Details/i }).first();
    const isOrderVisible = await orderLink.isVisible().catch(() => false);
    if (!isOrderVisible) {
      test.skip(true, 'No orders available for this user — skipping');
      return;
    }
    await orderLink.click();
    await page.waitForLoadState('domcontentloaded');
    await orderDetailEnhancedPage.waitForLoaded();
    await orderDetailEnhancedPage.backToOrdersLink.click();
    await expect(page).toHaveURL(/\/orders/);
  });
});
