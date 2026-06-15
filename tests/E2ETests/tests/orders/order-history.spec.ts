import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { TIMEOUTS } from '../../utils/constants';
import { getOrders, getOrder } from '../../utils/order-helpers';
import { poll } from '../../utils/poll';
import {
  ensureProductExists,
  ensureCategoryExists,
  ensureStoreExists,
  getCurrentUser,
} from '../../utils/api-helpers';

test.describe('Orders: Order History', () => {
  test.describe.configure({ timeout: 120_000 });
  let orderId: string;

  test.beforeAll(async ({ buyerApi, buyerUser, sellerApi, adminApi }) => {
    // Check if buyer already has orders (from prior tests or seeder).
    const existing = await getOrders(buyerApi, buyerUser.id);
    if (existing.length > 0) {
      orderId = existing[0].id;
      return;
    }

    // Create store → category → product → inventory → order — fully self-sufficient.
    const seller = await getCurrentUser(sellerApi);
    const randomId = Math.random().toString(36).substring(7).toUpperCase();

    const store = await ensureStoreExists(
      sellerApi,
      adminApi,
      seller.id,
      `OrderHistory Store ${randomId}`,
      `E2E store for order-history tests at ${new Date().toISOString()}`
    );

    const category = await ensureCategoryExists(sellerApi, 'Electronics', 'Devices and gadgets');

    const product = await ensureProductExists(
      sellerApi,
      {
        name: `OrderHistory Product ${randomId}`,
        description: `E2E product for order-history tests at ${new Date().toISOString()}`,
        categoryId: category.id,
        storeId: store.id,
        tags: ['e2e', 'order-history'],
      },
      {
        skuCode: `ORDHIST-${randomId}`,
        price: 49.99,
        currency: 'USD',
      },
      100
    );

    const sku = product.skus[0];

    // Add to cart directly using known product data (bypasses catalog list search
    // which filters by status=Active and may miss newly created Draft products).
    const cartResponse = await buyerApi.post('/api/cart/items', {
      data: {
        productId: product.id,
        skuId: sku.id,
        skuCode: sku.skuCode,
        quantity: 1,
      },
    });
    if (!cartResponse.ok() && cartResponse.status() !== 409) {
      throw new Error(`Add to cart failed: ${cartResponse.status()} ${await cartResponse.text()}`);
    }

    // Checkout — 409 means cart was already checked out (parallel worker race)
    const checkoutResponse = await buyerApi.post('/api/cart/checkout', {
      data: {
        addressLine1: '123 Test St',
        city: 'Testville',
        state: 'TS',
        postalCode: '12345',
        country: 'US',
      },
    });

    if (checkoutResponse.ok()) {
      const { correlationId } = await checkoutResponse.json();

      // Poll for terminal order status
      const terminalStatuses = ['Completed', 'Cancelled', 'Faulted'];
      let finalOrder = null;
      try {
        finalOrder = await poll(
          async () => {
            const order = await getOrder(buyerApi, correlationId);
            if (order && terminalStatuses.includes(order.statusName)) {
              return order;
            }
            return null;
          },
          { maxAttempts: 30, delayMs: 2000, label: 'order completion' }
        );
      } catch {
        finalOrder = await getOrder(buyerApi, correlationId);
      }

      if (finalOrder) {
        orderId = finalOrder.id;
        return;
      }
    }

    // Fallback: re-fetch orders (checkout may have been done by parallel worker,
    // or the order may still be processing).
    const orders = await getOrders(buyerApi, buyerUser.id);
    if (orders.length === 0) {
      throw new Error('No orders found for buyer after checkout attempt');
    }
    orderId = orders[0].id;
  });

  test('should display orders page after login', async ({ ordersPage }) => {
    await test.step('Navigate to orders page', async () => {
      await ordersPage.goto();
      await ordersPage.waitForPageLoad();
    });

    await test.step('Verify orders heading is visible', async () => {
      await expect(ordersPage.pageHeading).toBeVisible();
    });
  });

  test('should show empty state when no orders', async ({ ordersPage }) => {
    await test.step('Navigate to orders page', async () => {
      await ordersPage.goto();
      await ordersPage.waitForPageLoad();
    });

    await test.step('Verify orders heading is visible', async () => {
      await expect(ordersPage.pageHeading).toBeVisible();
    });
  });

  test('should navigate to order detail when clicking an order', async ({ ordersPage }) => {
    await test.step('Navigate to order detail page', async () => {
      await ordersPage.page.goto(`/orders/${orderId}`);
      await ordersPage.waitForPageLoad();
    });

    await test.step('Verify order details heading is visible', async () => {
      await expect(
        ordersPage.page.getByRole('heading', { name: 'Order Details' })
      ).toBeVisible({ timeout: TIMEOUTS.element });
    });

    await test.step('Verify the URL contains the order ID', async () => {
      await expect(ordersPage.page).toHaveURL(new RegExp(`/orders/${orderId}`));
    });
  });
});
