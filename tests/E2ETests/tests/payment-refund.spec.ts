import { test, expect, APIRequestContext } from '@playwright/test';
import {
  createTestData,
  runCheckoutFlow,
  poll,
  getPaymentByOrderId,
  refundPayment,
  type TestDataSetup,
} from '../utils/api-helpers';

let data: TestDataSetup;
let transactionId: string;
let orderId: string;

test.describe('Payment Refund Flow', () => {
  test.beforeAll(async ({ request }) => {
    // Create full test data environment (mirrors seeder pipeline)
    data = await createTestData(request, {
      productCount: 1,
      stockPerProduct: 50,
      productPrice: 49.99,
    });

    // Run checkout flow to create an order with payment
    const { correlationId, finalOrder } = await runCheckoutFlow(
      data.buyerApi,
      [{ sku: data.products[0].sku, quantity: 1, price: data.products[0].price, shopId: data.store.sellerId }],
      {
        addressLine1: '123 Refund St',
        city: 'Testville',
        state: 'TS',
        postalCode: '12345',
        country: 'US',
      }
    );

    orderId = correlationId;
    expect(orderId).toBeTruthy();

    // Poll for payment completion
    const payment = await poll(
      async () => {
        const resp = await getPaymentByOrderId(data.buyerApi, orderId);
        if (!resp) return null;
        if (resp.status === 'Completed' || resp.status === 'Refunded') return resp;
        return null;
      },
      { maxAttempts: 20, delayMs: 1000, label: 'payment completion' }
    );

    transactionId = payment.id;
    expect(transactionId).toBeTruthy();
  });

  test.afterAll(async () => {
    await data?.buyerApi?.dispose();
    await data?.sellerApi?.dispose();
    await data?.adminApi?.dispose();
  });

  test('admin can refund a completed payment', async () => {
    expect(transactionId).toBeTruthy();

    const result = await refundPayment(data.adminApi, transactionId, 'Customer requested refund');
    expect(result.refundId).toBeTruthy();
  });

  test('refund record appears in payment query', async () => {
    expect(orderId).toBeTruthy();

    const payment = await getPaymentByOrderId(data.buyerApi, orderId);
    expect(payment).toBeTruthy();
    expect(payment.refunds).toBeTruthy();
    expect(payment.refunds.length).toBeGreaterThan(0);

    const refund = payment.refunds[0];
    expect(refund.status).toBe('Processed');
    expect(refund.amount).toBe(49.99);
    expect(refund.reason).toBe('Customer requested refund');
  });

  test('cannot refund already refunded transaction', async () => {
    expect(transactionId).toBeTruthy();

    const response = await data.adminApi.post(`/api/payments/${transactionId}/refund`, {
      data: { reason: 'Second attempt' },
    });

    expect(response.status()).toBe(400);
    const body = await response.json();
    expect(body.error).toContain('refunded');
  });

  test('buyer sees refunded status in payment query', async () => {
    expect(orderId).toBeTruthy();

    const payment = await getPaymentByOrderId(data.buyerApi, orderId);
    expect(payment).toBeTruthy();
    expect(payment.status).toBe('Refunded');
  });

  test('non-admin cannot issue refund', async () => {
    expect(transactionId).toBeTruthy();

    const response = await data.buyerApi.post(`/api/payments/${transactionId}/refund`, {
      data: { reason: 'Unauthorized attempt' },
    });

    expect(response.status()).toBe(403);
  });
});
