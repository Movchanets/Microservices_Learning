/**
 * Barrel re-export for backward compatibility.
 *
 * All helpers have been split into focused modules:
 *   - types.ts          — shared interfaces
 *   - poll.ts           — polling utility
 *   - auth-helpers.ts   — login, register, user management, browser auth
 *   - store-helpers.ts  — store CRUD, verification, idempotent ensure
 *   - catalog-helpers.ts — products, categories, inventory
 *   - cart-helpers.ts   — cart operations
 *   - order-helpers.ts  — orders, checkout flow
 *   - payment-helpers.ts — payments, refunds
 *   - test-data.ts      — high-level test data builder
 *
 * New code should import from the specific module directly.
 */

// Types
export type {
  BffUser,
  StoreResult,
  ProductResult,
  ProductListResult,
  SkuResult,
  CategoryResult,
  OrderResult,
  OrderItemResult,
  InventoryResult,
} from './types';

// Polling
export { poll } from './poll';
export type { PollOptions } from './poll';

// Auth
export {
  loginApi,
  registerApi,
  getCurrentUser,
  getUsers,
  getUserByEmail,
  promoteToSeller,
  ensureUserExists,
  ensureAuthenticatedPage,
  ensureAuthenticatedPageViaApi,
} from './auth-helpers';
export type { AuthenticatedPage } from './auth-helpers';

// Store
export {
  createStore,
  verifyStore,
  getStoreBySellerId,
  getStores,
  ensureStoreExists,
} from './store-helpers';

// Catalog & Inventory
export {
  createProduct,
  addSku,
  getProductById,
  getProductBySku,
  activateProduct,
  getCategories,
  createCategory,
  ensureCategoryExists,
  ensureProductExists,
  createInventoryItem,
  setInventoryStock,
  getInventoryItem,
} from './catalog-helpers';

// Cart
export { addToCart } from './cart-helpers';

// Orders
export { getOrder, getOrders, cancelOrder, runCheckoutFlow } from './order-helpers';

// Payments
export { getPaymentByOrderId, refundPayment } from './payment-helpers';

// Test data builder
export { createTestData } from './test-data';
export type { TestDataSetup, TestDataSetupOptions } from './test-data';
