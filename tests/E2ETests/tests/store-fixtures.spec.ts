import { storeTest as test, expect } from '../fixtures/store.fixture';

test.describe('Store Fixtures', () => {
  test('should create and verify a store via API', async ({ testStore }) => {
    expect(testStore.id).toBeTruthy();
    expect(testStore.name).toContain('Test Store');
    expect(testStore.verificationStatus).toBe('Verified');
  });

  test('should create a product in the verified store via API', async ({ testStore, testProduct }) => {
    expect(testProduct.id).toBeTruthy();
    expect(testProduct.storeId).toBe(testStore.id);
    expect(testProduct.name).toContain('Test Product');
    expect(testProduct.sku).toMatch(/^TEST-/);
    expect(testProduct.status).toBe('Draft');
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
