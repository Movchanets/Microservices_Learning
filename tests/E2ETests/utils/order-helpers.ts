/**
 * Order API helpers and checkout flow.
 */

import { APIRequestContext } from '@playwright/test';
import type { OrderResult } from './types';
import { addToCart } from './cart-helpers';
import { poll, type PollOptions } from './poll';

export async function getOrder(
  api: APIRequestContext,
  orderId: string
): Promise<OrderResult | null> {
  const response = await api.get(`/bff/orders/${orderId}`);
  if (response.status() === 404) return null;
  if (!response.ok()) {
    throw new Error(`Get order failed: ${response.status()} ${await response.text()}`);
  }
  const data = await response.json();
  return {
    ...data,
    statusName: data.statusName ?? data.status,
    status: typeof data.status === 'number' ? data.status : 0,
  };
}

export async function getOrders(
  api: APIRequestContext
): Promise<OrderResult[]> {
  const response = await api.get('/api/orders');
  if (!response.ok()) {
    throw new Error(`Get orders failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function cancelOrder(
  api: APIRequestContext,
  orderId: string,
  reason: string
): Promise<boolean> {
  const response = await api.post(`/api/orders/${orderId}/cancel`, {
    data: { reason },
  });
  return response.ok();
}

/**
 * Full checkout flow: add items to cart → checkout → poll for completion.
 * Mirrors OrderFlowSeeder.RunOrderFlowAsync
 */
export async function runCheckoutFlow(
  buyerApi: APIRequestContext,
  items: Array<{ sku: string; quantity: number; price: number; shopId?: string }>,
  address: {
    addressLine1: string;
    city: string;
    state: string;
    postalCode: string;
    country: string;
  },
  pollOptions?: PollOptions
): Promise<{ correlationId: string; finalOrder: OrderResult | null }> {
  // 1. Add items to cart
  for (const item of items) {
    await addToCart(buyerApi, item.sku, item.quantity, item.price, item.shopId);
  }

  // 2. Checkout
  const checkoutResponse = await buyerApi.post('/api/cart/checkout', { data: address });
  if (!checkoutResponse.ok()) {
    const err = await checkoutResponse.text();
    throw new Error(`Checkout failed: ${checkoutResponse.status()} ${err}`);
  }

  const checkoutResult = await checkoutResponse.json();
  const correlationId = checkoutResult.correlationId;
  if (!correlationId) {
    throw new Error('Checkout returned no correlationId');
  }

  // 3. Poll for terminal order status
  const terminalStatuses = ['Completed', 'Cancelled', 'Faulted'];
  let finalOrder: OrderResult | null = null;

  try {
    finalOrder = await poll(
      async () => {
        const order = await getOrder(buyerApi, correlationId);
        console.log(`[runCheckoutFlow Poll] Correlation ID: ${correlationId}, Status: ${order?.statusName ?? 'null'}`);
        if (order && terminalStatuses.includes(order.statusName)) {
          return order;
        }
        return null;
      },
      { maxAttempts: 30, delayMs: 2000, label: 'order completion', ...pollOptions }
    );
  } catch {
    // Order didn't reach terminal state in time — return what we have
    finalOrder = await getOrder(buyerApi, correlationId);
  }

  return { correlationId, finalOrder };
}
