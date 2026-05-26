/**
 * E2E Tests: Store Creation + Product/SKU CRUD
 *
 * Tests the full seller workflow via API:
 *   1. Create a store (seller)
 *   2. Verify the store (admin)
 *   3. Create a product (no SKU initially)
 *   4. Add multiple SKUs to the product
 *   5. Verify SKUs are returned with the product
 *   6. Change a SKU's price
 *   7. Remove a SKU
 *   8. Activate the product
 *   9. Delete the product
 *
 * Uses the auth fixture for pre-authenticated seller/admin API contexts.
 */

import { authTest as test, expect } from '../../fixtures/auth.fixture';
import {
  createStore,
  verifyStore,
  getStoreBySellerId,
  createProduct,
  addSku,
  getProductById,
  activateProduct,
  ensureCategoryExists,
} from '../../utils/api-helpers';
import type { StoreResult, ProductResult, SkuResult } from '../../utils/types';

test.describe('Seller: Product & SKU CRUD', () => {
  let store: StoreResult;
  let categoryId: string;
  const uniqueId = Math.random().toString(36).substring(7).toUpperCase();

  test.beforeAll(async ({ sellerApi, sellerUser, adminApi }) => {
    // Create and verify a store
    store = await createStore(sellerApi, sellerUser.id, `Test Store ${uniqueId}`, 'E2E CRUD test store');
    await verifyStore(adminApi, store.id, true);

    // Ensure a category exists
    const category = await ensureCategoryExists(adminApi, 'Electronics', 'Devices and gadgets');
    categoryId = category.id;
  });

  test('should create a product without SKUs', async ({ sellerApi }) => {
    const product = await createProduct(sellerApi, {
      name: `CRUD Product ${uniqueId}`,
      description: 'Product for testing CRUD operations',
      categoryId,
      storeId: store.id,
      tags: ['e2e', 'crud-test'],
    });

    expect(product).toBeTruthy();
    expect(product.id).toBeTruthy();
    expect(product.name).toBe(`CRUD Product ${uniqueId}`);
    expect(product.storeId).toBe(store.id);
    expect(product.skus).toEqual([]);
  });

  test('should add multiple SKUs to a product', async ({ sellerApi }) => {
    // Create a fresh product for this test
    const product = await createProduct(sellerApi, {
      name: `Multi-SKU Product ${uniqueId}`,
      description: 'Product with multiple SKUs',
      categoryId,
      storeId: store.id,
    });

    // Add first SKU (e.g., Small Red)
    const sku1 = await addSku(sellerApi, product.id, {
      skuCode: `MS-${uniqueId}-S-RED`,
      price: 29.99,
      currency: 'USD',
      typedAttributes: { color: 'Red', size: 'S' },
    });

    expect(sku1.id).toBeTruthy();
    expect(sku1.skuCode).toBe(`MS-${uniqueId}-S-RED`);
    expect(sku1.price).toBe(29.99);
    expect(sku1.currency).toBe('USD');

    // Add second SKU (e.g., Large Blue)
    const sku2 = await addSku(sellerApi, product.id, {
      skuCode: `MS-${uniqueId}-L-BLU`,
      price: 34.99,
      currency: 'USD',
      typedAttributes: { color: 'Blue', size: 'L' },
    });

    expect(sku2.id).toBeTruthy();
    expect(sku2.skuCode).toBe(`MS-${uniqueId}-L-BLU`);
    expect(sku2.price).toBe(34.99);

    // Verify the product now has both SKUs
    const fetched = await getProductById(sellerApi, product.id);
    expect(fetched).toBeTruthy();
    expect(fetched!.skus).toHaveLength(2);

    const skuCodes = fetched!.skus.map(s => s.skuCode);
    expect(skuCodes).toContain(`MS-${uniqueId}-S-RED`);
    expect(skuCodes).toContain(`MS-${uniqueId}-L-BLU`);
  });

  test('should reject duplicate SKU code on same product', async ({ sellerApi }) => {
    const product = await createProduct(sellerApi, {
      name: `Dup SKU Product ${uniqueId}`,
      description: 'Tests duplicate SKU rejection',
      categoryId,
      storeId: store.id,
    });

    // Add first SKU
    await addSku(sellerApi, product.id, {
      skuCode: `DUP-${uniqueId}-01`,
      price: 10.00,
      currency: 'USD',
    });

    // Try to add duplicate — should fail
    const response = await sellerApi.post(`/api/catalog/products/${product.id}/skus`, {
      data: { skuCode: `DUP-${uniqueId}-01`, price: 15.00, currency: 'USD' },
    });

    expect(response.ok()).toBe(false);
    expect(response.status()).toBeGreaterThanOrEqual(400);
  });

  test('should change a SKU price', async ({ sellerApi }) => {
    const product = await createProduct(sellerApi, {
      name: `Price Change Product ${uniqueId}`,
      description: 'Tests price change',
      categoryId,
      storeId: store.id,
    });

    const sku = await addSku(sellerApi, product.id, {
      skuCode: `PC-${uniqueId}-01`,
      price: 50.00,
      currency: 'USD',
    });

    // Change price
    const priceResponse = await sellerApi.patch(
      `/api/catalog/products/${product.id}/skus/${sku.id}/price`,
      { data: { price: 75.00, currency: 'USD' } }
    );
    expect(priceResponse.ok()).toBe(true);

    // Verify price changed
    const fetched = await getProductById(sellerApi, product.id);
    expect(fetched).toBeTruthy();
    const updatedSku = fetched!.skus.find(s => s.id === sku.id);
    expect(updatedSku).toBeTruthy();
    expect(updatedSku!.price).toBe(75.00);
  });

  test('should remove a SKU from a product', async ({ sellerApi }) => {
    const product = await createProduct(sellerApi, {
      name: `Remove SKU Product ${uniqueId}`,
      description: 'Tests SKU removal',
      categoryId,
      storeId: store.id,
    });

    const sku1 = await addSku(sellerApi, product.id, {
      skuCode: `RM-${uniqueId}-01`,
      price: 10.00,
      currency: 'USD',
    });

    const sku2 = await addSku(sellerApi, product.id, {
      skuCode: `RM-${uniqueId}-02`,
      price: 20.00,
      currency: 'USD',
    });

    // Verify both SKUs exist
    let fetched = await getProductById(sellerApi, product.id);
    expect(fetched!.skus).toHaveLength(2);

    // Remove first SKU
    const deleteResponse = await sellerApi.delete(
      `/api/catalog/products/${product.id}/skus/${sku1.id}`
    );
    expect(deleteResponse.ok()).toBe(true);

    // Verify only one SKU remains
    fetched = await getProductById(sellerApi, product.id);
    expect(fetched!.skus).toHaveLength(1);
    expect(fetched!.skus[0].skuCode).toBe(`RM-${uniqueId}-02`);
  });

  test('should activate a product with active SKUs', async ({ sellerApi }) => {
    const product = await createProduct(sellerApi, {
      name: `Activate Product ${uniqueId}`,
      description: 'Tests activation with SKUs',
      categoryId,
      storeId: store.id,
    });

    // Add a SKU (required for activation)
    await addSku(sellerApi, product.id, {
      skuCode: `ACT-${uniqueId}-01`,
      price: 25.00,
      currency: 'USD',
    });

    // Activate
    const activateResponse = await sellerApi.put(
      `/api/catalog/products/${product.id}/activate`,
      { data: {} }
    );
    expect(activateResponse.ok()).toBe(true);

    // Verify status
    const fetched = await getProductById(sellerApi, product.id);
    expect(fetched!.status).toBe('Active');
  });

  test('should delete a product', async ({ sellerApi }) => {
    const product = await createProduct(sellerApi, {
      name: `Delete Product ${uniqueId}`,
      description: 'Tests product deletion',
      categoryId,
      storeId: store.id,
    });

    // Verify it exists
    let fetched = await getProductById(sellerApi, product.id);
    expect(fetched).toBeTruthy();

    // Delete
    const deleteResponse = await sellerApi.delete(
      `/api/catalog/products/${product.id}`
    );
    expect(deleteResponse.ok()).toBe(true);

    // Verify it's gone (soft-deleted — returns null or 404)
    fetched = await getProductById(sellerApi, product.id);
    expect(fetched).toBeNull();
  });

  test('should return products with minPrice/maxPrice/skuCount in list', async ({ sellerApi }) => {
    // Create product with multiple price points
    const product = await createProduct(sellerApi, {
      name: `Price Range Product ${uniqueId}`,
      description: 'Tests list price aggregation',
      categoryId,
      storeId: store.id,
    });

    await addSku(sellerApi, product.id, {
      skuCode: `PR-${uniqueId}-LOW`,
      price: 10.00,
      currency: 'USD',
    });

    await addSku(sellerApi, product.id, {
      skuCode: `PR-${uniqueId}-HIGH`,
      price: 99.99,
      currency: 'USD',
    });

    // Fetch the product list filtered by store
    const listResponse = await sellerApi.get('/api/catalog/products', {
      params: { storeId: store.id },
    });
    expect(listResponse.ok()).toBe(true);

    const listData = await listResponse.json();
    const items = listData.items ?? listData;
    const found = items.find((p: { id: string }) => p.id === product.id);

    expect(found).toBeTruthy();
    expect(found.minPrice).toBe(10.00);
    expect(found.maxPrice).toBe(99.99);
    expect(found.skuCount).toBe(2);
  });
});
