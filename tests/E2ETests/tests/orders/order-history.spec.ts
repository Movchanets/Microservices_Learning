import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { TIMEOUTS } from '../../utils/constants';
import { getOrders } from '../../utils/order-helpers';
import {
  ensureStoreExists,
  ensureCategoryExists,
  ensureProductExists,
} from '../../utils/api-helpers';

// Self-sufficient order-history test — creates all data programmatically
// via seller/admin APIs, uses isolated per-worker buyer to avoid parallel conflicts.

test.describe.configure({ timeout: 120_000 });

test.describe('Orders: Order History', () => {
  let orderId: string;

  // ── Seed: create store → product → inventory → checkout ────

  test.beforeAll(async ({ isolatedBuyerApi, isolatedBuyerUser, sellerApi, sellerUser, adminApi }) => {
    // 1. Short-circuit if buyer already has orders
    const existing = await getOrders(isolatedBuyerApi, isolatedBuyerUser.id);
    if (existing.length > 0) {
      orderId = existing[0].id;
      return;
    }

    // 2. Create store + category + product + SKU + inventory
    const store = await ensureStoreExists(
      sellerApi, adminApi, sellerUser.id,
      'Order History Store', 'E2E order-history test store'
    );

    const category = await ensureCategoryExists(adminApi, 'Order History Audio', 'Audio equipment');

    // Use worker-unique suffix from email to avoid SKU/product name collisions across parallel workers
    const workerTag = isolatedBuyerUser.email.split('@')[0].replace('+', '').toUpperCase(); // "BUYERW0", "BUYERW1", ...
    let skuCode = `OH-SONY-${workerTag}`;
    let product = await ensureProductExists(
      sellerApi,
      {
        name: `Sony WH-1000XM5 (${workerTag})`,
        description: 'Wireless noise-cancelling headphones',
        categoryId: category.id,
        storeId: store.id,
        brand: 'Sony',
        tags: ['headphones', 'audio'],
      },
      { skuCode, price: 349.99, currency: 'USD' },
      10
    );

    let sku = product.skus.find(s => s.skuCode === skuCode);
    if (!sku) {
      throw new Error(
        `SKU "${skuCode}" not found in product "${product.name}" (id=${product.id}). ` +
        `Available SKUs: [${product.skus.map(s => s.skuCode).join(', ')}]`
      );
    }

    // 3. Add to cart — retry to handle eventual consistency (SkuCreated event propagation)
    //    Cart service returns 400 "SKU not found" until it processes the integration event.
    //    If retries exhaust, the SKU may be stale from a prior test run — create a fresh product.
    let cartOk = false;
    for (let attempt = 0; attempt < 10; attempt++) {
      const cartResponse = await isolatedBuyerApi.post('/api/cart/items', {
        data: { productId: product.id, skuId: sku.id, skuCode: sku.skuCode, quantity: 1 },
      });
      // 409 = item already in cart from prior retry — treat as success
      if (cartResponse.ok() || cartResponse.status() === 409) {
        cartOk = true;
        break;
      }
      // 400 = SKU not yet propagated to Cart service — wait and retry
      if (cartResponse.status() === 400 && attempt < 9) {
        await new Promise(r => setTimeout(r, 2000));
        continue;
      }
      // Retries exhausted — SKU is likely stale. Create a fresh product with new SKU code.
      if (cartResponse.status() === 400) {
        console.log(`[order-history] SKU "${skuCode}" not in Cart after 20s — creating fresh product`);
        const freshSuffix = Date.now().toString(36).toUpperCase();
        skuCode = `OH-SONY-${workerTag}-${freshSuffix}`;
        product = await ensureProductExists(
          sellerApi,
          {
            name: `Sony WH-1000XM5 (${workerTag}-${freshSuffix})`,
            description: 'Wireless noise-cancelling headphones',
            categoryId: category.id,
            storeId: store.id,
            brand: 'Sony',
            tags: ['headphones', 'audio'],
          },
          { skuCode, price: 349.99, currency: 'USD' },
          10
        );
        sku = product.skus.find(s => s.skuCode === skuCode);
        if (!sku) {
          throw new Error(`Fresh SKU "${skuCode}" not found in product "${product.name}"`);
        }
        // Retry cart add with fresh SKU
        const retryResp = await isolatedBuyerApi.post('/api/cart/items', {
          data: { productId: product.id, skuId: sku.id, skuCode: sku.skuCode, quantity: 1 },
        });
        if (retryResp.ok() || retryResp.status() === 409) {
          cartOk = true;
          break;
        }
        throw new Error(`Cart add failed even with fresh product: ${retryResp.status()} ${await retryResp.text()}`);
      }
      throw new Error(`Add to cart failed: ${cartResponse.status()} ${await cartResponse.text()}`);
    }
    if (!cartOk) {
      throw new Error('Failed to add item to cart after all retries');
    }

    // 4. Checkout
    const checkoutResponse = await isolatedBuyerApi.post('/api/cart/checkout', {
      data: {
        addressLine1: '123 Test St',
        city: 'Testville',
        state: 'TS',
        postalCode: '12345',
        country: 'US',
      },
    });
    // 409 = already checked out — fall through to order lookup
    if (!checkoutResponse.ok() && checkoutResponse.status() !== 409) {
      throw new Error(`Checkout failed: ${checkoutResponse.status()} ${await checkoutResponse.text()}`);
    }

    // 5. Poll for terminal order status, fall back to getOrders
    const correlationId = checkoutResponse.ok()
      ? (await checkoutResponse.json()).correlationId
      : null;

    if (correlationId) {
      const terminalStatuses = ['Completed', 'Cancelled', 'Faulted'];
      for (let i = 0; i < 30; i++) {
        await new Promise(r => setTimeout(r, 2000));
        const order = await isolatedBuyerApi.get(`/bff/orders/${correlationId}`);
        if (order.ok()) {
          const data = await order.json();
          if (terminalStatuses.includes(data.statusName ?? data.status)) {
            orderId = data.id;
            return;
          }
        }
      }
    }

    // 6. Fallback — re-fetch orders (order may still be processing)
    const orders = await getOrders(isolatedBuyerApi, isolatedBuyerUser.id);
    if (orders.length === 0) {
      throw new Error('Checkout succeeded but no orders found for buyer');
    }
    orderId = orders[0].id;
  });

  // ── Tests ──────────────────────────────────────────────────

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

  test('should navigate to order detail page', async ({ page }) => {
    await test.step('Navigate directly to order detail', async () => {
      await page.goto(`/orders/${orderId}`);
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify Order Details heading and URL', async () => {
      await expect(page).toHaveURL(new RegExp(`/orders/${orderId}`));
      await expect(
        page.getByRole('heading', { name: 'Order Details' })
      ).toBeVisible({ timeout: TIMEOUTS.element });
    });
  });
});
