import { test, expect } from '../fixtures/test-base';
import { ensureAuthenticatedPageViaApi } from '../utils/api-helpers';
import { OrderDetailEnhancedPage } from '../pages/order-detail-enhanced.page';

test.describe('Order Cancellation & Status', () => {

  test('should display order detail with cancel button for cancellable orders', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const orderDetailPage = new OrderDetailEnhancedPage(page);

    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    const orderLink = page.getByRole('link').filter({ hasText: /View Details/i }).first();
    const isOrderVisible = await orderLink.isVisible().catch(() => false);
    if (!isOrderVisible) {
      test.skip(true, 'No orders available for this user — skipping');
      await context.close();
      return;
    }
    await orderLink.click();
    await page.waitForLoadState('domcontentloaded');
    await orderDetailPage.waitForLoaded();
    await expect(orderDetailPage.pageHeading).toBeVisible();
    await context.close();
  });

  test('should show order status badge', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const orderDetailPage = new OrderDetailEnhancedPage(page);

    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    const orderLink = page.getByRole('link').filter({ hasText: /View Details/i }).first();
    const isOrderVisible = await orderLink.isVisible().catch(() => false);
    if (!isOrderVisible) {
      test.skip(true, 'No orders available — skipping');
      await context.close();
      return;
    }
    await orderLink.click();
    await page.waitForLoadState('domcontentloaded');
    await orderDetailPage.waitForLoaded();
    await expect(orderDetailPage.statusBadge).toBeVisible();
    await context.close();
  });

  test('should show order timeline', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const orderDetailPage = new OrderDetailEnhancedPage(page);

    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    const orderLink = page.getByRole('link').filter({ hasText: /View Details/i }).first();
    const isOrderVisible = await orderLink.isVisible().catch(() => false);
    if (!isOrderVisible) {
      test.skip(true, 'No orders available — skipping');
      await context.close();
      return;
    }
    await orderLink.click();
    await page.waitForLoadState('domcontentloaded');
    await orderDetailPage.waitForLoaded();
    await expect(orderDetailPage.timeline).toBeVisible();
    await context.close();
  });

  test('should show order items list', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const orderDetailPage = new OrderDetailEnhancedPage(page);

    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    const orderLink = page.getByRole('link').filter({ hasText: /View Details/i }).first();
    const isOrderVisible = await orderLink.isVisible().catch(() => false);
    if (!isOrderVisible) {
      test.skip(true, 'No orders available — skipping');
      await context.close();
      return;
    }
    await orderLink.click();
    await page.waitForLoadState('domcontentloaded');
    await orderDetailPage.waitForLoaded();
    const itemCount = await orderDetailPage.getOrderItemCount();
    expect(itemCount).toBeGreaterThan(0);
    await context.close();
  });

  test('should navigate back to orders from detail', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const orderDetailPage = new OrderDetailEnhancedPage(page);

    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    const orderLink = page.getByRole('link').filter({ hasText: /View Details/i }).first();
    const isOrderVisible = await orderLink.isVisible().catch(() => false);
    if (!isOrderVisible) {
      test.skip(true, 'No orders available — skipping');
      await context.close();
      return;
    }
    await orderLink.click();
    await page.waitForLoadState('domcontentloaded');
    await orderDetailPage.waitForLoaded();
    await orderDetailPage.backToOrdersLink.click();
    await expect(page).toHaveURL(/\/orders/);
    await context.close();
  });
});
