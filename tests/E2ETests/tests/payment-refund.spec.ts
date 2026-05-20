import { test, expect, APIRequestContext } from '@playwright/test';
import {
  loginApi,
  registerApi,
  createStore,
  verifyStore,
  createProduct,
  getCategories,
  addToCart,
  getCurrentUser,
} from '../utils/api-helpers';

let buyerApi: APIRequestContext;
let sellerApi: APIRequestContext;
let adminApi: APIRequestContext;
let orderId: string;
let transactionId: string;

/**
 * Polls an async condition with exponential backoff.
 * Returns the first truthy result, or throws after maxAttempts.
 */
async function poll<T>(
  fn: () => Promise<T>,
  { maxAttempts = 20, delayMs = 1000, label = 'condition' } = {}
): Promise<T> {
  for (let i = 0; i < maxAttempts; i++) {
    const result = await fn();
    if (result) return result;
    console.log(`Polling ${label}... attempt ${i + 1}/${maxAttempts}`);
    await new Promise((r) => setTimeout(r, delayMs));
  }
  throw new Error(`Polling ${label} timed out after ${maxAttempts} attempts`);
}

test.describe('Payment Refund Flow', () => {
  test.beforeAll(async ({ request }) => {
    // Register and login buyer
    buyerApi = await registerApi(
      request,
      'Refund',
      'Buyer',
      `refund-buyer-${Date.now()}@test.com`,
      'Test123!'
    );

    // Register and login seller
    sellerApi = await registerApi(
      request,
      'Refund',
      'Seller',
      `refund-seller-${Date.now()}@test.com`,
      'Test123!'
    );

    // Login as admin (pre-seeded)
    adminApi = await loginApi(request, 'admin@marketplace.com', 'P@ssw0rd123!');

    // Create store and verify it
    const sellerUser = await getCurrentUser(sellerApi);
    const store = await createStore(sellerApi, sellerUser.id, 'Refund Test Store', 'Test store for refunds');
    await verifyStore(adminApi, store.id, true);

    // Create product
    const categories = await getCategories(sellerApi);
    const category = categories[0];
    const product = await createProduct(sellerApi, {
      name: 'Refund Test Product',
      description: 'A product for testing refunds',
      sku: `REFUND-TEST-${Date.now()}`,
      price: 49.99,
      currency: 'USD',
      categoryId: category.id,
      storeId: store.id,
    });

    // Add to cart and checkout
    await addToCart(buyerApi, product.sku, 1, product.price, store.sellerId);

    const checkoutResponse = await buyerApi.post('/api/cart/checkout', {
      data: {
        addressLine1: '123 Refund St',
        city: 'Testville',
        state: 'TS',
        postalCode: '12345',
        country: 'US',
      },
    });

    expect(checkoutResponse.ok()).toBeTruthy();
    const checkoutResult = await checkoutResponse.json();
    orderId = checkoutResult.correlationId;
    expect(orderId).toBeTruthy();

    // Poll for payment completion instead of fixed sleep
    const payment = await poll(
      async () => {
        const resp = await buyerApi.get(`/api/payments/order/${orderId}`);
        if (!resp.ok()) return null;
        const body = await resp.json();
        // Wait until payment is Completed or Refunded
        if (body.status === 'Completed' || body.status === 'Refunded') return body;
        return null;
      },
      { maxAttempts: 20, delayMs: 1000, label: 'payment completion' }
    );

    transactionId = payment.id;
    expect(transactionId).toBeTruthy();
  });

  test.afterAll(async () => {
    await buyerApi?.dispose();
    await sellerApi?.dispose();
    await adminApi?.dispose();
  });

  test('admin can refund a completed payment', async () => {
    expect(transactionId).toBeTruthy();

    const response = await adminApi.post(`/api/payments/${transactionId}/refund`, {
      data: { reason: 'Customer requested refund' },
    });

    expect(response.status()).toBe(201);
    const body = await response.json();
    expect(body.refundId).toBeTruthy();
  });

  test('refund record appears in payment query', async () => {
    expect(orderId).toBeTruthy();

    const response = await buyerApi.get(`/api/payments/order/${orderId}`);
    expect(response.ok()).toBeTruthy();

    const body = await response.json();
    expect(body.refunds).toBeTruthy();
    expect(body.refunds.length).toBeGreaterThan(0);

    const refund = body.refunds[0];
    expect(refund.status).toBe('Processed');
    expect(refund.amount).toBe(49.99);
    expect(refund.reason).toBe('Customer requested refund');
  });

  test('cannot refund already refunded transaction', async () => {
    expect(transactionId).toBeTruthy();

    const response = await adminApi.post(`/api/payments/${transactionId}/refund`, {
      data: { reason: 'Second attempt' },
    });

    expect(response.status()).toBe(400);
    const body = await response.json();
    expect(body.error).toContain('refunded');
  });

  test('buyer sees refunded status in payment query', async () => {
    expect(orderId).toBeTruthy();

    const response = await buyerApi.get(`/api/payments/order/${orderId}`);
    expect(response.ok()).toBeTruthy();

    const body = await response.json();
    expect(body.status).toBe('Refunded');
  });

  test('non-admin cannot issue refund', async () => {
    expect(transactionId).toBeTruthy();

    // Buyer should not be able to refund
    const response = await buyerApi.post(`/api/payments/${transactionId}/refund`, {
      data: { reason: 'Unauthorized attempt' },
    });

    expect(response.status()).toBe(403);
  });
});
