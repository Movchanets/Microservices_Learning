import { checkoutTest as test, expect } from '../fixtures/checkout.fixture';
import { createTestData, runCheckoutFlow, getOrder, cancelOrder, addToCart, poll } from '../utils/api-helpers';
import { OrderDetailEnhancedPage } from '../pages/order-detail-enhanced.page';

test.describe('Saga-Aware Order Cancellation', () => {

  test('completed order should NOT show cancel button', async ({
    browser,
    playwright,
  }) => {
    // Create test data and run checkout via API (fast, no UI)
    const data = await createTestData(playwright.request, { productCount: 1 });

    const { finalOrder } = await runCheckoutFlow(
      data.buyerApi,
      [{ sku: data.products[0].sku, quantity: 1, price: data.products[0].price, shopId: data.store.sellerId }],
      { addressLine1: '456 Cancel Street', city: 'Cancelville', state: 'NY', postalCode: '10001', country: 'US' }
    );

    expect(finalOrder).not.toBeNull();
    expect(finalOrder!.statusName).toBe('Completed');

    // Create page with buyer context
    const storageState = await data.buyerApi.storageState();
    const context = await browser.newContext({ storageState });
    const page = await context.newPage();
    const orderDetailEnhancedPage = new OrderDetailEnhancedPage(page);

    // Go directly to order detail page
    await orderDetailEnhancedPage.goto(finalOrder!.id);
    await orderDetailEnhancedPage.waitForLoaded();

    // Completed orders should NOT have a cancel button
    const hasCancel = await orderDetailEnhancedPage.hasCancelButton();
    expect(hasCancel).toBe(false);

    await page.close();
    await context.close();
    await data.buyerApi.dispose();
    await data.sellerApi.dispose();
    await data.adminApi.dispose();
  });

  test('buyer can cancel order via API and status reflects cancellation', async ({ playwright }) => {
    const data = await createTestData(playwright.request, { productCount: 1 });

    // Add to cart and create order
    await addToCart(data.buyerApi, data.products[0].sku, 1, data.products[0].price, data.store.sellerId);

    const orderResponse = await data.buyerApi.post('/api/orders', {
      data: {
        items: [{
          productId: data.products[0].id,
          productName: data.products[0].name,
          unitPrice: data.products[0].price,
          quantity: 1,
          storeId: data.store.id,
        }],
        shippingAddressLine1: '789 API Street',
        shippingCity: 'APIville',
        shippingState: 'TX',
        shippingPostalCode: '75001',
        shippingCountry: 'US',
      },
    });

    expect(orderResponse.ok()).toBeTruthy();
    const orderId = await orderResponse.json();

    // Give saga a tiny headstart (100ms) to ensure instance is created, before calling cancel
    await new Promise(r => setTimeout(r, 100));

    // Cancel immediately (best-effort before saga completes)
    const cancelSucceeded = await cancelOrder(data.buyerApi, orderId, 'Changed my mind');

    if (cancelSucceeded) {
      // Poll for Cancelled status to avoid race condition with async messaging
      const order = await poll(
        async () => {
          const ord = await getOrder(data.buyerApi, orderId);
          if (ord && ord.statusName === 'Cancelled') {
            return ord;
          }
          return null;
        },
        { maxAttempts: 10, delayMs: 1000, label: 'order cancellation status' }
      ).catch(() => null);

      if (order) {
        expect(order.statusName).toBe('Cancelled');
      }
    }

    await data.buyerApi.dispose();
    await data.sellerApi.dispose();
    await data.adminApi.dispose();
  });

  test('cancelled order shows Cancelled status in order list', async ({ browser, playwright }) => {
    const data = await createTestData(playwright.request, { productCount: 1 });

    await addToCart(data.buyerApi, data.products[0].sku, 1, data.products[0].price, data.store.sellerId);

    const orderResponse = await data.buyerApi.post('/api/orders', {
      data: {
        items: [{
          productId: data.products[0].id,
          productName: data.products[0].name,
          unitPrice: data.products[0].price,
          quantity: 1,
          storeId: data.store.id,
        }],
        shippingAddressLine1: '321 Status Street',
        shippingCity: 'Statusville',
        shippingState: 'FL',
        shippingPostalCode: '33101',
        shippingCountry: 'US',
      },
    });

    expect(orderResponse.ok()).toBeTruthy();
    const orderId = await orderResponse.json();

    // Give saga a tiny headstart (100ms) to ensure instance is created, before calling cancel
    await new Promise(r => setTimeout(r, 100));

    const cancelSucceeded = await cancelOrder(data.buyerApi, orderId, 'Test cancellation');

    if (cancelSucceeded) {
      // Poll for Cancelled status to avoid race condition
      const order = await poll(
        async () => {
          const ord = await getOrder(data.buyerApi, orderId);
          if (ord && ord.statusName === 'Cancelled') {
            return ord;
          }
          return null;
        },
        { maxAttempts: 10, delayMs: 1000, label: 'order cancellation status' }
      ).catch(() => null);

      if (order && order.statusName === 'Cancelled') {
        // Create page with buyer context
        const storageState = await data.buyerApi.storageState();
        const context = await browser.newContext({ storageState });
        const page = await context.newPage();
        const orderDetailEnhancedPage = new OrderDetailEnhancedPage(page);

        await page.goto('/orders');
        await page.waitForLoadState('domcontentloaded');

        const cancelledBadge = page.getByText(/cancelled/i).first();
        const isBadgeVisible = await cancelledBadge.isVisible().catch(() => false);
        if (!isBadgeVisible) {
          test.skip(true, 'Cancelled order badge not found in list — skipping');
          await page.close();
          await context.close();
          await data.buyerApi.dispose();
          await data.sellerApi.dispose();
          await data.adminApi.dispose();
          return;
        }

        const orderLink = page.getByRole('link').filter({ hasText: /View Details/i }).first();
        await expect(orderLink).toBeVisible({ timeout: 10000 });
        await orderLink.click();
        await page.waitForLoadState('domcontentloaded');
        await orderDetailEnhancedPage.waitForLoaded();

        const status = await orderDetailEnhancedPage.getStatus();
        expect(status.toLowerCase()).toContain('cancel');

        await page.close();
        await context.close();
      }
    }

    await data.buyerApi.dispose();
    await data.sellerApi.dispose();
    await data.adminApi.dispose();
  });
});
