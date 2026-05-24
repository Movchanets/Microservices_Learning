/**
 * Payment API helpers.
 */

import { APIRequestContext } from '@playwright/test';

export async function getPaymentByOrderId(
  api: APIRequestContext,
  orderId: string
): Promise<any | null> {
  const response = await api.get(`/api/payments/order/${orderId}`);
  if (response.status() === 404) return null;
  if (!response.ok()) {
    throw new Error(`Get payment failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function refundPayment(
  api: APIRequestContext,
  transactionId: string,
  reason: string
): Promise<{ refundId: string }> {
  const response = await api.post(`/api/payments/${transactionId}/refund`, {
    data: { reason },
  });
  if (!response.ok()) {
    throw new Error(`Refund failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}
