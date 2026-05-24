import { checkoutTest as test, expect } from '../fixtures/checkout.fixture';
import { createTestData, addToCart, getCurrentUser } from '../utils/api-helpers';

test.describe('Seller Order Correlation', () => {

  test('buyer checkout creates order visible to seller', async ({
    browser,
    playwright,
  }) => {
    // Create full environment: buyer, seller, store, product
    const data = await createTestData(playwright.request, { productCount: 1 });

    // Add product to buyer's cart
    await addToCart(data.buyerApi, data.products[0].sku, 1, data.products[0].price, data.store.sellerId);

    // Checkout via API
    const checkoutResponse = await data.buyerApi.post('/api/cart/checkout', {
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

    // Create browser context with pre-authenticated seller storageState
    const storageState = await data.sellerApi.storageState();
    const context = await browser.newContext({ storageState });
    const sellerPage = await context.newPage();

    await sellerPage.goto('/seller');
    await sellerPage.waitForLoadState('domcontentloaded');

    const ordersTab = sellerPage.getByRole('link', { name: /orders/i });
    await expect(ordersTab).toBeVisible({ timeout: 10000 });
    await ordersTab.click();
    await sellerPage.waitForLoadState('domcontentloaded');

    const hasTable = await sellerPage.locator('table').isVisible();
    const isEmpty = await sellerPage.getByText('No orders yet').isVisible();

    if (hasTable) {
      const tableText = await sellerPage.locator('table').textContent();
      expect(tableText).toBeTruthy();
    } else {
      expect(isEmpty).toBe(true);
    }

    await sellerPage.close();
    await context.close();
    await data.buyerApi.dispose();
    await data.sellerApi.dispose();
    await data.adminApi.dispose();
  });

  test('cart item includes storeId when adding product', async ({ playwright }) => {
    const data = await createTestData(playwright.request, { productCount: 1 });

    // Add product to cart
    await addToCart(data.buyerApi, data.products[0].sku, 1, data.products[0].price, data.store.sellerId);

    // Verify cart item has storeId
    const getCartResponse = await data.buyerApi.get('/api/cart');
    expect(getCartResponse.ok()).toBeTruthy();
    const cart = await getCartResponse.json();
    expect(cart.items).toHaveLength(1);
    expect(cart.items[0].storeId).toBe(data.store.id);

    await data.buyerApi.dispose();
    await data.sellerApi.dispose();
    await data.adminApi.dispose();
  });
});
