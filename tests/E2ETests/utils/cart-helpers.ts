/**
 * Cart API helpers.
 */

import { APIRequestContext } from '@playwright/test';
import { getProductBySku } from './catalog-helpers';

/**
 * Adds an item to the cart via the Cart API.
 * Uses the `POST /api/cart/` endpoint which accepts items with explicit prices,
 * bypassing the ProductPrices event-sync dependency.
 */
export async function addToCart(
  api: APIRequestContext,
  sku: string,
  quantity: number,
  price: number,
  shopId?: string
): Promise<void> {
  const product = await getProductBySku(api, sku);
  if (!product) {
    throw new Error(`Product not found for SKU: ${sku}`);
  }

  const response = await api.post('/api/cart/items', {
    data: { productId: product.id, quantity },
  });

  if (!response.ok()) {
    const body = await response.text();
    console.error(`Add to cart failed: ${response.status()} ${body}`);
    throw new Error(`Add to cart failed: ${response.status()} ${body}`);
  }

  // Verify the cart actually has items
  const verifyResponse = await api.get('/api/cart');
  if (verifyResponse.ok()) {
    const cart = await verifyResponse.json();
    console.log(`Cart verified: ${cart.items?.length ?? 0} items`);
  }
}
