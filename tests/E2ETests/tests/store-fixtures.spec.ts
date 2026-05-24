import { storeTest as test, expect } from '../fixtures/store.fixture';
import { ensureStoreExists, ensureProductExists, ensureCategoryExists, getCurrentUser } from '../utils/api-helpers';

test.describe('Store Fixtures', () => {
  test('should create and verify a store via ensure helpers', async ({ sellerApi, adminApi }) => {
    const seller = await getCurrentUser(sellerApi);
    const store = await ensureStoreExists(sellerApi, adminApi, seller.id, 'Ensure Test Store', 'Created by ensureStoreExists');

    expect(store.id).toBeTruthy();
    expect(store.name).toContain('Ensure Test Store');
    expect(store.verificationStatus).toBe('Verified');
  });

  test('should create a product with inventory via ensure helpers', async ({ sellerApi, adminApi, testStore }) => {
    const category = await ensureCategoryExists(adminApi, 'Electronics', 'Devices and gadgets');
    const randomId = Math.random().toString(36).substring(7).toUpperCase();

    const product = await ensureProductExists(sellerApi, {
      name: `Ensure Product ${randomId}`,
      description: 'Created by ensureProductExists',
      sku: `ENSURE-${randomId}`,
      price: 29.99,
      currency: 'USD',
      categoryId: category.id,
      storeId: testStore.id,
      tags: ['e2e', 'test'],
    }, 100);

    expect(product.id).toBeTruthy();
    expect(product.name).toContain('Ensure Product');
    expect(product.sku).toMatch(/^ENSURE-/);
  });

  test('should be idempotent — calling ensure twice returns same store', async ({ sellerApi, adminApi, testStore }) => {
    const seller = await getCurrentUser(sellerApi);

    // Call ensureStoreExists again with the same seller
    const store2 = await ensureStoreExists(sellerApi, adminApi, seller.id, testStore.name, 'Second call');

    // Should return the same store, not create a duplicate
    expect(store2.id).toBe(testStore.id);
  });

  test('should provide authenticated seller API context', async ({ sellerApi }) => {
    const response = await sellerApi.get('/bff/user');
    expect(response.ok()).toBeTruthy();

    const user = await response.json();
    expect(user.email).toBe('store.tech@marketplace.com');
    expect(user.role).toBe('Seller');
  });

  test('should provide authenticated admin API context', async ({ adminApi }) => {
    const response = await adminApi.get('/bff/user');
    expect(response.ok()).toBeTruthy();

    const user = await response.json();
    expect(user.email).toBe('admin@marketplace.com');
    expect(user.role).toBe('Admin');
  });
});
