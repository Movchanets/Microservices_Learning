import { test as base, APIRequestContext } from '@playwright/test';
import { test as pageTest } from './test-base';
import {
  loginApi,
  createStore,
  verifyStore,
  createProduct,
  addToCart,
  getCategories,
  getStoreBySellerId,
  getCurrentUser,
  type StoreResult,
  type ProductResult,
} from '../utils/api-helpers';
import * as users from '../data/users.json';

/**
 * Combined fixture for checkout E2E tests.
 * Merges page object fixtures from test-base with store/product API fixtures.
 */
export type CheckoutFixtures = {
  /** Authenticated API context for the seller user */
  sellerApi: APIRequestContext;
  /** Authenticated API context for the admin user */
  adminApi: APIRequestContext;
  /** A store that has been created by the seller and approved by admin */
  testStore: StoreResult;
  /** A draft product created in the verified test store */
  testProduct: ProductResult;
  /** Adds the test product to a buyer's cart via API */
  addItemToCart: (buyerApi: APIRequestContext, quantity?: number) => Promise<void>;
};

// Merge page fixtures with store fixtures
export const checkoutTest = pageTest.extend<CheckoutFixtures>({
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
        store = await getStoreBySellerId(sellerApi, seller.id);
        if (!store) {
          throw new Error(`Failed to create or find store for seller ${seller.id}`);
        }
      }
    }

    if (store.verificationStatus !== 'Verified') {
      try {
        await verifyStore(adminApi, store.id, true);
      } catch {
        // 409 if already verified
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

    await use(product);
  },

  addItemToCart: async ({ testProduct, testStore }, use) => {
    const fn = async (buyerApi: APIRequestContext, quantity = 1) => {
      await addToCart(buyerApi, testProduct.sku, quantity, testProduct.price, testStore.sellerId);
    };
    await use(fn);
  },
});

export { expect } from '@playwright/test';
