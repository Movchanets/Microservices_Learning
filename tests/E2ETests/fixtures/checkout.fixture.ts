import { APIRequestContext } from '@playwright/test';
import {
  ensureStoreExists,
  ensureProductExists,
  ensureCategoryExists,
  getCurrentUser,
  addToCart,
  type StoreResult,
  type ProductResult,
} from '../utils/api-helpers';
import { authTest as baseTest, type AuthFixtures } from './auth.fixture';

/**
 * Combined fixture for checkout E2E tests.
 * Uses idempotent "ensure" helpers mirroring the Seeder.App pipeline.
 *
 * Provides pre-authenticated buyer/seller/admin contexts, a verified store,
 * and a product with inventory — so tests focus on the feature under test.
 */
export type CheckoutFixtures = AuthFixtures & {
  /** A store created by the seller and approved by admin */
  testStore: StoreResult;
  /** A product with inventory in the verified test store */
  testProduct: ProductResult;
  /** Adds the test product to a buyer's cart via API */
  addItemToCart: (buyerApi: APIRequestContext, quantity?: number) => Promise<void>;
};

export const checkoutTest = baseTest.extend<Omit<CheckoutFixtures, keyof AuthFixtures>>({
  testStore: async ({ sellerApi, adminApi }, use) => {
    const seller = await getCurrentUser(sellerApi);
    const randomId = Math.random().toString(36).substring(7).toUpperCase();

    const store = await ensureStoreExists(
      sellerApi,
      adminApi,
      seller.id,
      `Test Store ${randomId}`,
      `E2E test store created at ${new Date().toISOString()}`
    );

    await use(store);
  },

  testProduct: async ({ sellerApi, testStore }, use) => {
    const categories = await ensureCategoryExists(
      sellerApi,
      'Electronics',
      'Devices and gadgets'
    );

    const randomId = Math.random().toString(36).substring(7).toUpperCase();

    const product = await ensureProductExists(
      sellerApi,
      {
        name: `Test Product ${randomId}`,
        description: `E2E test product created at ${new Date().toISOString()}`,
        categoryId: categories.id,
        storeId: testStore.id,
        tags: ['e2e', 'test'],
      },
      {
        skuCode: `TEST-${randomId}`,
        price: 29.99,
        currency: 'USD',
      },
      100
    );

    await use(product);
  },

  addItemToCart: async ({ testProduct, testStore }, use) => {
    const fn = async (buyerApi: APIRequestContext, quantity = 1) => {
      const firstSku = testProduct.skus[0];
      await addToCart(buyerApi, firstSku.skuCode, quantity, firstSku.price, testStore.sellerId);
    };
    await use(fn);
  },
});

export { expect } from '@playwright/test';
