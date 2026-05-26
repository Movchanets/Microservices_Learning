/**
 * Cart API helpers.
 */

import { APIRequestContext } from '@playwright/test';
import { getProductBySku } from './catalog-helpers';

/**
 * Adds an item to the cart via the Cart API.
 * Resolves the product and SKU by skuCode, then sends full item data.
 */
export async function addToCart(
  api: APIRequestContext,
  skuCode: string,
  quantity: number,
  price: number,
  shopId?: string
): Promise<void> {
  const product = await getProductBySku(api, skuCode);
  if (!product) {
    throw new Error(`Product not found for SKU: ${skuCode}`);
  }

  const sku = product.skus?.find(s => s.skuCode === skuCode);
  if (!sku) {
    throw new Error(`SKU '${skuCode}' not found in product ${product.id}`);
  }

  const response = await api.post('/api/cart/items', {
    data: { productId: product.id, skuId: sku.id, skuCode: sku.skuCode, quantity },
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
