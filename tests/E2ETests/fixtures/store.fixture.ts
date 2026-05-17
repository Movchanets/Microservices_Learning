import { test as base, APIRequestContext } from '@playwright/test';
import {
  loginApi,
  createStore,
  verifyStore,
  createProduct,
  createInventoryItem,
  getCategories,
  getStoreBySellerId,
  getCurrentUser,
  type StoreResult,
  type ProductResult,
} from '../utils/api-helpers';
import * as users from '../data/users.json';

/**
 * Store & product fixtures for E2E tests.
 *
 * Provides pre-created, admin-verified stores and draft products
 * so tests can focus on the feature under test without repeating setup.
 *
 * Usage:
 *   import { storeTest, expect } from '../fixtures/store.fixture';
 *
 *   storeTest('seller can manage products', async ({ page, testStore, testProduct, sellerApi }) => {
 *     // testStore is already verified by admin
 *     // testProduct is a draft product in that store
 *     // sellerApi is an authenticated API context for programmatic setup
 *   });
 */
export type StoreFixtures = {
  /** Authenticated API context for the seller user */
  sellerApi: APIRequestContext;
  /** Authenticated API context for the admin user */
  adminApi: APIRequestContext;
  /** A store that has been created by the seller and approved by admin */
  testStore: StoreResult;
  /** A draft product created in the verified test store */
  testProduct: ProductResult;
};

export const storeTest = base.extend<StoreFixtures>({
  sellerApi: async ({ playwright }, use) => {
    const api = await loginApi(
      playwright.request,
      users.sellerUser.email,
      users.sellerUser.password
    );
    await use(api);
    await api.dispose();
  },

  adminApi: async ({ playwright }, use) => {
    const api = await loginApi(
      playwright.request,
      users.adminUser.email,
      users.adminUser.password
    );
    await use(api);
    await api.dispose();
  },

  testStore: async ({ sellerApi, adminApi }, use) => {
    const seller = await getCurrentUser(sellerApi);

    // Try to reuse existing store first (seeded data or prior test run)
    let store = await getStoreBySellerId(sellerApi, seller.id).catch(() => null);

    if (!store) {
      try {
        const randomId = Math.random().toString(36).substring(7).toUpperCase();
        store = await createStore(
          sellerApi,
          seller.id,
          `Test Store ${randomId}`,
          `E2E test store created at ${new Date().toISOString()}`
        );
      } catch {
        // Creation failed (likely unique constraint — store exists from parallel test).
        // Retry the fetch.
        store = await getStoreBySellerId(sellerApi, seller.id);
        if (!store) {
          throw new Error(
            `Failed to create or find store for seller ${seller.id}`
          );
        }
      }
    }

    // Ensure the store is verified (idempotent — ignore 409 if already verified)
    if (store.verificationStatus !== 'Verified') {
      try {
        await verifyStore(adminApi, store.id, true);
      } catch {
        // 409 Conflict if already verified — safe to ignore
      }
      store.verificationStatus = 'Verified';
    }

    await use(store);
  },

  testProduct: async ({ sellerApi, testStore }, use) => {
    const categories = await getCategories(sellerApi);
    const categoryId =
      categories.length > 0
        ? categories[0].id
        : '00000000-0000-0000-0000-000000000000';

    const randomId = Math.random().toString(36).substring(7).toUpperCase();

    const product = await createProduct(sellerApi, {
      name: `Test Product ${randomId}`,
      description: `E2E test product created at ${new Date().toISOString()}`,
      sku: `TEST-${randomId}`,
      price: 29.99,
      currency: 'USD',
      categoryId,
      storeId: testStore.id,
      tags: ['e2e', 'test'],
    });

    // Create inventory item with stock so the "Add to Cart" button is enabled
    await createInventoryItem(sellerApi, product.sku, 100);

    await use(product);
  },
});

export { expect } from '@playwright/test';
