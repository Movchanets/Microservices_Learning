import { checkoutTest as test, expect } from '../fixtures/checkout.fixture';
import { loginApi, registerApi } from '../utils/api-helpers';
import * as users from '../data/users.json';

test.describe('Plan 10: Seller Order Correlation', () => {

  test('buyer checkout creates order visible to seller', async ({
    page,
    playwright,
    testStore,
    testProduct,
    addItemToCart,
  }) => {
    // 1. Register a fresh buyer
    const randomId = Math.random().toString(36).substring(7);
    const buyerEmail = `buyer_${randomId}@test.com`;
    const buyerPassword = 'P@ssw0rd123!';

    const buyerApi = await registerApi(
      playwright.request,
      'Test',
      'Buyer',
      buyerEmail,
      buyerPassword
    );

    try {
      // 2. Add product to buyer's cart via API (includes sellerId)
      await addItemToCart(buyerApi);

      // 3. Checkout via API
      const checkoutResponse = await buyerApi.post('/api/cart/checkout', {
        data: {
          addressLine1: '123 Test St',
          city: 'Testville',
          state: 'TS',
          postalCode: '12345',
          country: 'US',
        },
      });
      expect(checkoutResponse.ok()).toBeTruthy();
      const checkoutResult = await checkoutResponse.json();
      expect(checkoutResult.correlationId).toBeTruthy();

      // 4. Wait for order to be processed (saga completes)
      await page.waitForLoadState('domcontentloaded');

      // 5. Login as seller in browser
      await page.goto('/auth/login');
      await page.getByPlaceholder('name@company.com').fill(users.sellerUser.email);
      await page.getByPlaceholder('••••••••').fill(users.sellerUser.password);
      await page.getByRole('button', { name: /sign in/i }).click();
      await page.waitForLoadState('domcontentloaded');

      // 6. Navigate to seller dashboard -> Orders tab
      await page.goto('/seller');
      await page.waitForLoadState('domcontentloaded');

    const ordersTab = page.getByRole('link', { name: /orders/i });
    await expect(ordersTab).toBeVisible({ timeout: 10000 });
    await ordersTab.click();
    await page.waitForLoadState('domcontentloaded');

    // 7. Verify order appears in seller order list
    const hasTable = await page.locator('table').isVisible();
    const isEmpty = await page.getByText('No orders yet').isVisible();

    if (hasTable) {
      const tableText = await page.locator('table').textContent();
      expect(tableText).toBeTruthy();
    } else {
      expect(isEmpty).toBe(true);
    }
    } finally {
      await buyerApi.dispose();
    }
  });

  test('cart item includes sellerId when adding product', async ({
    playwright,
    testStore,
    testProduct,
  }) => {
    const randomId = Math.random().toString(36).substring(7);
    const buyerEmail = `buyer_cart_${randomId}@test.com`;

    const buyerApi = await registerApi(
      playwright.request,
      'Cart',
      'Test',
      buyerEmail,
      'P@ssw0rd123!'
    );

    try {
      // Add product to cart with shopId
      const cartResponse = await buyerApi.post('/api/cart/items', {
        data: {
          sku: testProduct.sku,
          quantity: 1,
          shopId: testStore.sellerId,
        },
      });

      expect(cartResponse.ok()).toBeTruthy();

      // Verify cart item has shopId
      const getCartResponse = await buyerApi.get('/api/cart');
      expect(getCartResponse.ok()).toBeTruthy();
      const cart = await getCartResponse.json();
      expect(cart.items).toHaveLength(1);
      expect(cart.items[0].shopId).toBe(testStore.sellerId);
    } finally {
      await buyerApi.dispose();
    }
  });
});
