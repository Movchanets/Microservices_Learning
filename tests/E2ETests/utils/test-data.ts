/**
 * High-level test data builder.
 * Mirrors the full Seeder.App pipeline in a single call.
 */

import { APIRequestContext } from '@playwright/test';
import type { BffUser, StoreResult, ProductResult, CategoryResult } from './types';
import { loginApi, getCurrentUser, promoteToSeller, ensureUserExists } from './auth-helpers';
import { ensureStoreExists } from './store-helpers';
import { ensureCategoryExists, ensureProductExists } from './catalog-helpers';

export interface TestDataSetup {
  /** Authenticated API context for the buyer */
  buyerApi: APIRequestContext;
  /** Authenticated API context for the seller */
  sellerApi: APIRequestContext;
  /** Authenticated API context for the seller (alias/fresh context) */
  sellerApiFresh: APIRequestContext;
  /** Authenticated API context for the admin */
  adminApi: APIRequestContext;
  /** The seller's verified store */
  store: StoreResult;
  /** Products created with inventory */
  products: ProductResult[];
  /** Categories used */
  categories: CategoryResult[];
  /** Current buyer user info */
  buyer: BffUser;
  /** Current seller user info */
  seller: BffUser;
}

export interface TestDataSetupOptions {
  /** Number of products to create (default: 2) */
  productCount?: number;
  /** Stock per product (default: 100) */
  stockPerProduct?: number;
  /** Product price (default: 29.99) */
  productPrice?: number;
  /** Store name (default: random) */
  storeName?: string;
  /** Store description (default: auto) */
  storeDescription?: string;
  /** Category name to use (default: first available) */
  categoryName?: string;
}

/**
 * Creates a complete test data environment mirroring the Seeder.App pipeline.
 *
 * Pipeline:
 *   1. Register/login buyer + seller
 *   2. Login as admin (pre-seeded)
 *   3. Create + verify store for seller
 *   4. Ensure category exists
 *   5. Create N products with inventory
 *
 * Returns everything a test needs to write assertions against.
 */
export async function createTestData(
  requestFactory: APIRequestContext,
  options: TestDataSetupOptions = {}
): Promise<TestDataSetup> {
  const {
    productCount = 2,
    stockPerProduct = 100,
    productPrice = 29.99,
    storeDescription = 'E2E test store',
  } = options;

  const uniqueId = Math.random().toString(36).substring(7);

  // 1. Register/login users
  const buyerEmail = `e2e-buyer-${uniqueId}@test.com`;
  const sellerEmail = `e2e-seller-${uniqueId}@test.com`;
  const password = 'P@ssw0rd123!';

  const buyerApi = await ensureUserExists(requestFactory, 'E2E', 'Buyer', buyerEmail, password);
  const sellerApi = await ensureUserExists(requestFactory, 'E2E', 'Seller', sellerEmail, password);
  const adminApi = await loginApi(requestFactory, 'admin@marketplace.com', 'P@ssw0rd123!');

  const buyer = await getCurrentUser(buyerApi);
  const seller = await getCurrentUser(sellerApi);

  // 2. Promote seller to Seller role (via admin), then re-login for fresh JWT
  try {
    await promoteToSeller(adminApi, seller.id);
  } catch (e) {
    // Already a seller (409) is OK; other errors should be logged
    console.warn(`[createTestData] promoteToSeller warning: ${e}`);
  }

  // Re-login seller to get a JWT with the Seller role claim
  await sellerApi.dispose();
  const sellerApiFresh = await loginApi(requestFactory, sellerEmail, password);

  // 3. Create + verify store
  const storeName = options.storeName ?? `E2E Store ${uniqueId.toUpperCase()}`;
  const store = await ensureStoreExists(
    sellerApiFresh,
    adminApi,
    seller.id,
    storeName,
    storeDescription
  );

  // 4. Ensure category
  const categoryName = options.categoryName ?? 'Electronics';
  const category = await ensureCategoryExists(adminApi, categoryName, 'Test category');

  // 5. Create products with inventory
  const products: ProductResult[] = [];
  for (let i = 0; i < productCount; i++) {
    const sku = `E2E-${uniqueId.toUpperCase()}-${i + 1}`;
    const product = await ensureProductExists(
      sellerApiFresh,
      {
        name: `E2E Product ${i + 1} (${uniqueId})`,
        description: `E2E test product #${i + 1}`,
        sku,
        price: productPrice,
        currency: 'USD',
        categoryId: category.id,
        storeId: store.id,
        tags: ['e2e', 'test'],
      },
      stockPerProduct
    );
    products.push(product);
  }

  return {
    buyerApi,
    sellerApi: sellerApiFresh,
    sellerApiFresh,
    adminApi,
    store,
    products,
    categories: [category],
    buyer,
    seller,
  };
}
