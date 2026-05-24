/**
 * Test data fixture — provides a complete seeded environment
 * mirroring the Seeder.App pipeline.
 *
 * Usage:
 *   import { testDataSetupTest as test, expect } from '../fixtures/test-data.fixture';
 *
 *   test('can add to cart', async ({ testData }) => {
 *     expect(testData.store.id).toBeTruthy();
 *     expect(testData.products.length).toBeGreaterThan(0);
 *   });
 */

import { APIRequestContext } from '@playwright/test';
import { authTest as baseTest, type AuthFixtures } from './auth.fixture';
import {
  createTestData,
  addToCart,
  runCheckoutFlow,
  type TestDataSetup,
  type ProductResult,
  type StoreResult,
  type OrderResult,
} from '../utils/api-helpers';

// Re-export for convenience
export type { TestDataSetup, ProductResult, StoreResult, OrderResult };

export type TestDataFixtures = AuthFixtures & {
  /** Full test data environment: users, store, products, categories */
  testData: TestDataSetup;
  /** First product from testData.products */
  testProduct: ProductResult;
  /** Store from testData.store */
  testStore: StoreResult;
  /** Adds testProduct to a buyer's cart via API */
  addItemToCart: (buyerApi: APIRequestContext, quantity?: number) => Promise<void>;
};

export const testDataSetupTest = baseTest.extend<Omit<TestDataFixtures, keyof AuthFixtures>>({
  testData: async ({ playwright }, use) => {
    const data = await createTestData(playwright.request, {
      productCount: 2,
      stockPerProduct: 100,
      productPrice: 29.99,
    });
    await use(data);
    // Cleanup
    await data.buyerApi.dispose();
    await data.sellerApi.dispose();
    await data.adminApi.dispose();
  },

  testStore: async ({ testData }, use) => {
    await use(testData.store);
  },

  testProduct: async ({ testData }, use) => {
    await use(testData.products[0]);
  },

  addItemToCart: async ({ testData }, use) => {
    const fn = async (buyerApi: APIRequestContext, quantity = 1) => {
      await addToCart(
        buyerApi,
        testData.products[0].sku,
        quantity,
        testData.products[0].price,
        testData.store.sellerId
      );
    };
    await use(fn);
  },
});

export { expect } from '@playwright/test';
