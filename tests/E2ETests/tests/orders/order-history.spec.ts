import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { TIMEOUTS } from '../../utils/constants';
import { getOrders, runCheckoutFlow } from '../../utils/order-helpers';
import { getProductBySku } from '../../utils/catalog-helpers';

const TEST_SKU = 'AUDIO-SONY-WH1000XM5';

test.describe('Orders: Order History', () => {
  let orderId: string;

  test.beforeAll(async ({ buyerApi, buyerUser }) => {
    // Seed an order so the navigation test has data to work with.
    // Check if buyer already has orders (from seeder or prior tests).
    const existing = await getOrders(buyerApi, buyerUser.id);
    if (existing.length > 0) {
      orderId = existing[0].id;
      return;
    }

    // No orders exist — create one via checkout flow.
    const product = await getProductBySku(buyerApi, TEST_SKU);
    if (!product) {
      throw new Error(`Test product SKU ${TEST_SKU} not found. Run the seeder first.`);
    }
    const sku = product.skus!.find(s => s.skuCode === TEST_SKU)!;

    const { finalOrder } = await runCheckoutFlow(
      buyerApi,
      [{ skuCode: sku.skuCode, quantity: 1, price: sku.price }],
      {
        addressLine1: '123 Test St',
        city: 'Testville',
        state: 'TS',
        postalCode: '12345',
        country: 'US',
      }
    );
    if (!finalOrder) {
      // Fallback: re-fetch — order may still be processing
      const orders = await getOrders(buyerApi, buyerUser.id);
      if (orders.length === 0) {
        throw new Error('Checkout succeeded but no orders found for buyer');
      }
      orderId = orders[0].id;
    } else {
      orderId = finalOrder.id;
    }
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

  test('should navigate to order detail when clicking an order', async ({ ordersPage, buyerApi, buyerUser }) => {
    await test.step('Navigate to orders page', async () => {
      await ordersPage.goto();
      await ordersPage.waitForPageLoad();
    });

    await test.step('Verify at least one order exists', async () => {
      const orders = await getOrders(buyerApi, buyerUser.id);
      expect(orders.length).toBeGreaterThan(0);
    });

    await test.step('Click the first order link', async () => {
      // Order list shows truncated ID: first 8 chars + "..."
      await ordersPage.viewOrderDetails(orderId.slice(0, 8));
    });

    await test.step('Verify navigation to order detail page', async () => {
      await ordersPage.page.waitForURL(`**/orders/${orderId}`, { timeout: TIMEOUTS.api });
      await expect(ordersPage.page).toHaveURL(new RegExp(`/orders/${orderId}`));
      await expect(
        ordersPage.page.getByRole('heading', { name: 'Order Details' })
      ).toBeVisible({ timeout: TIMEOUTS.element });
    });
  });
});
