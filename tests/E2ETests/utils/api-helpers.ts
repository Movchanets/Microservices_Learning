import { APIRequestContext, Browser, BrowserContext, Page } from '@playwright/test';

/**
 * Logs in via the BFF endpoint and returns a new authenticated API context.
 * Each call creates isolated request contexts to avoid cookie pollution
 * when logging in multiple users in the same test.
 *
 * Handles CSRF token extraction automatically.
 */
export async function loginApi(
  requestFactory: APIRequestContext,
  email: string,
  password: string
): Promise<APIRequestContext> {
  const baseUrl = process.env.BASE_URL || 'http://localhost:4200';

  const tempCtx = await (requestFactory as any).newContext({ baseURL: baseUrl }) as APIRequestContext;

  const loginResponse = await tempCtx.post(`${baseUrl}/bff/auth/login`, {
    data: { email, password },
  });

  if (!loginResponse.ok()) {
    const body = await loginResponse.text();
    await tempCtx.dispose();
    throw new Error(
      `Login failed for ${email}: ${loginResponse.status()} ${body}`
    );
  }

  const state = await tempCtx.storageState();
  await tempCtx.dispose();

  const xsrfCookie = state.cookies.find((c) => c.name === 'XSRF-TOKEN');
  const xsrfToken = xsrfCookie?.value ?? '';

  const context = await (requestFactory as any).newContext({
    baseURL: baseUrl,
    storageState: state,
    extraHTTPHeaders: {
      'X-XSRF-TOKEN': xsrfToken,
    },
  }) as APIRequestContext;

  return context;
}

// ── Type interfaces ──

export interface BffUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
}

export interface StoreResult {
  id: string;
  sellerId: string;
  name: string;
  description: string;
  verificationStatus: string;
}

export interface ProductResult {
  id: string;
  name: string;
  sku: string;
  price: number;
  storeId: string;
  status: string;
}

export interface CategoryResult {
  id: string;
  name: string;
  slug: string;
}

export interface OrderResult {
  id: string;
  buyerId: string;
  status: number;
  statusName: string;
  totalAmount: number;
  createdAt: string;
  completedAt: string | null;
  items: OrderItemResult[];
}

export interface OrderItemResult {
  id: string;
  productId: string;
  productName: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

export interface InventoryResult {
  sku: string;
  availableQuantity: number;
}

// ── Store Management API ──

export async function createStore(
  api: APIRequestContext,
  sellerId: string,
  name: string,
  description: string
): Promise<StoreResult> {
  const response = await api.post('/api/stores', {
    data: { sellerId, name, description },
  });
  if (!response.ok()) {
    throw new Error(`Create store failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function verifyStore(
  api: APIRequestContext,
  storeId: string,
  isApproved: boolean,
  reason?: string
): Promise<void> {
  const response = await api.post(`/api/stores/${storeId}/verify`, {
    data: { isApproved, reason },
  });
  if (!response.ok()) {
    throw new Error(`Verify store failed: ${response.status()} ${await response.text()}`);
  }
}

export async function getStoreBySellerId(
  api: APIRequestContext,
  sellerId: string
): Promise<StoreResult | null> {
  const response = await api.get(`/api/stores/seller/${sellerId}`);
  if (response.status() === 404) return null;
  if (!response.ok()) {
    throw new Error(`Get store failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function getStores(api: APIRequestContext): Promise<StoreResult[]> {
  const response = await api.get('/api/stores');
  if (!response.ok()) {
    throw new Error(`Get stores failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

// ── Catalog API ──

export async function createProduct(
  api: APIRequestContext,
  product: {
    name: string;
    description: string;
    sku: string;
    price: number;
    currency: string;
    categoryId: string;
    storeId: string;
    tags?: string[];
    imageUrl?: string;
  }
): Promise<ProductResult> {
  const response = await api.post('/api/catalog/products', {
    data: product,
  });
  if (!response.ok()) {
    throw new Error(`Create product failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function getProductBySku(
  api: APIRequestContext,
  sku: string
): Promise<ProductResult | null> {
  const response = await api.get(`/api/catalog/products/sku/${sku}`);
  if (response.status() === 404) return null;
  if (!response.ok()) {
    throw new Error(`Get product by SKU failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function activateProduct(
  api: APIRequestContext,
  productId: string
): Promise<void> {
  const response = await api.put(`/api/catalog/products/${productId}/activate`);
  // 409 = already active, ignore
  if (!response.ok() && response.status() !== 409) {
    throw new Error(`Activate product failed: ${response.status()} ${await response.text()}`);
  }
}

export async function getCategories(
  api: APIRequestContext
): Promise<CategoryResult[]> {
  const response = await api.get('/api/catalog/categories');
  if (!response.ok()) {
    throw new Error(`Get categories failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function createCategory(
  api: APIRequestContext,
  name: string,
  description: string
): Promise<CategoryResult> {
  const response = await api.post('/api/catalog/categories', {
    data: { name, description },
  });
  if (!response.ok()) {
    throw new Error(`Create category failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

// ── Inventory API ──

export async function createInventoryItem(
  api: APIRequestContext,
  sku: string,
  initialQuantity: number
): Promise<void> {
  const response = await api.post('/api/inventory/items', {
    data: { sku, initialQuantity },
  });
  if (!response.ok()) {
    // Ignore 409 Conflict — item may already exist
    if (response.status() === 409) return;
    throw new Error(`Create inventory item failed: ${response.status()} ${await response.text()}`);
  }
}

export async function setInventoryStock(
  api: APIRequestContext,
  sku: string,
  quantity: number,
  storeId: string,
  productId: string
): Promise<void> {
  const response = await api.put(`/api/inventory/items/${sku}/stock`, {
    data: { quantity, storeId, productId },
  });
  if (!response.ok()) {
    throw new Error(`Set inventory stock failed: ${response.status()} ${await response.text()}`);
  }
}

export async function getInventoryItem(
  api: APIRequestContext,
  sku: string
): Promise<InventoryResult | null> {
  const response = await api.get(`/api/inventory/items/${sku}`);
  if (response.status() === 404) return null;
  if (!response.ok()) {
    throw new Error(`Get inventory failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

// ── Identity API ──

export async function getCurrentUser(
  api: APIRequestContext
): Promise<BffUser> {
  const response = await api.get('/bff/user');
  if (!response.ok()) {
    throw new Error(`Get user failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function getUsers(
  api: APIRequestContext
): Promise<BffUser[]> {
  const response = await api.get('/api/identity/users');
  if (!response.ok()) {
    throw new Error(`Get users failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function getUserByEmail(
  api: APIRequestContext,
  email: string
): Promise<BffUser | null> {
  const users = await getUsers(api);
  return users.find(u => u.email === email) ?? null;
}

export async function promoteToSeller(
  api: APIRequestContext,
  userId: string
): Promise<void> {
  const response = await api.put(`/api/identity/users/${userId}/role`, {
    data: { Role: 'Seller' },
  });
  if (!response.ok()) {
    // 409 = already seller
    if (response.status() === 409) return;
    throw new Error(`Promote to seller failed: ${response.status()} ${await response.text()}`);
  }
}

// ── Registration API ──

/**
 * Registers a new user via the BFF.
 */
export async function registerApi(
  requestFactory: APIRequestContext,
  firstName: string,
  lastName: string,
  email: string,
  password: string
): Promise<APIRequestContext> {
  const baseUrl = process.env.BASE_URL || 'http://localhost:4200';

  const tempCtx = await (requestFactory as any).newContext({ baseURL: baseUrl }) as APIRequestContext;

  const registerResponse = await tempCtx.post(`${baseUrl}/bff/auth/register`, {
    data: { firstName, lastName, email, password },
  });

  if (!registerResponse.ok()) {
    const body = await registerResponse.text();
    // If user already exists, just login instead
    if (registerResponse.status() === 400 && body.includes('already')) {
      await tempCtx.dispose();
      return loginApi(requestFactory, email, password);
    }
    await tempCtx.dispose();
    throw new Error(`Register failed: ${registerResponse.status()} ${body}`);
  }

  // Registration succeeded — extract cookies for the authenticated context
  const state = await tempCtx.storageState();
  await tempCtx.dispose();

  const xsrfCookie = state.cookies.find((c) => c.name === 'XSRF-TOKEN');
  const xsrfToken = xsrfCookie?.value ?? '';

  const context = await (requestFactory as any).newContext({
    baseURL: baseUrl,
    storageState: state,
    extraHTTPHeaders: {
      'X-XSRF-TOKEN': xsrfToken,
    },
  }) as APIRequestContext;

  return context;
}

// ── Cart API ──

/**
 * Adds an item to the cart via the Cart API.
 * Uses the `POST /api/cart/` endpoint which accepts items with explicit prices,
 * bypassing the ProductPrices event-sync dependency.
 */
export async function addToCart(
  api: APIRequestContext,
  sku: string,
  quantity: number,
  price: number,
  shopId?: string
): Promise<void> {
  // Get current cart to preserve existing items
  const cartResponse = await api.get('/api/cart');
  let existingItems: Array<{ sku: string; quantity: number; price: number; shopId?: string }> = [];

  if (cartResponse.ok()) {
    const cart = await cartResponse.json();
    existingItems = (cart.items || []).map((i: any) => ({
      sku: i.sku,
      quantity: i.quantity,
      price: i.price ?? 0,
      shopId: i.shopId ?? undefined,
    }));
  }

  // Add or update the new item
  const existingIndex = existingItems.findIndex((i) => i.sku === sku && i.shopId === shopId);
  if (existingIndex >= 0) {
    existingItems[existingIndex].quantity += quantity;
  } else {
    existingItems.push({ sku, quantity, price, shopId });
  }

  const response = await api.post('/api/cart/', {
    data: { items: existingItems },
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

// ── Order API ──

export async function getOrder(
  api: APIRequestContext,
  orderId: string
): Promise<OrderResult | null> {
  const response = await api.get(`/api/orders/${orderId}`);
  if (response.status() === 404) return null;
  if (!response.ok()) {
    throw new Error(`Get order failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function getOrders(
  api: APIRequestContext
): Promise<OrderResult[]> {
  const response = await api.get('/api/orders');
  if (!response.ok()) {
    throw new Error(`Get orders failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function cancelOrder(
  api: APIRequestContext,
  orderId: string,
  reason: string
): Promise<boolean> {
  const response = await api.post(`/api/orders/${orderId}/cancel`, {
    data: { reason },
  });
  return response.ok();
}

// ── Payment API ──

export async function getPaymentByOrderId(
  api: APIRequestContext,
  orderId: string
): Promise<any | null> {
  const response = await api.get(`/api/payments/order/${orderId}`);
  if (response.status() === 404) return null;
  if (!response.ok()) {
    throw new Error(`Get payment failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function refundPayment(
  api: APIRequestContext,
  transactionId: string,
  reason: string
): Promise<{ refundId: string }> {
  const response = await api.post(`/api/payments/${transactionId}/refund`, {
    data: { reason },
  });
  if (!response.ok()) {
    throw new Error(`Refund failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

// ── Polling Utility ──

export interface PollOptions {
  maxAttempts?: number;
  delayMs?: number;
  label?: string;
}

/**
 * Polls an async condition with configurable backoff.
 * Returns the first truthy result, or throws after maxAttempts.
 */
export async function poll<T>(
  fn: () => Promise<T>,
  options: PollOptions = {}
): Promise<T> {
  const { maxAttempts = 20, delayMs = 1000, label = 'condition' } = options;

  for (let i = 0; i < maxAttempts; i++) {
    const result = await fn();
    if (result) return result;
    console.log(`Polling ${label}... attempt ${i + 1}/${maxAttempts}`);
    await new Promise((r) => setTimeout(r, delayMs));
  }
  throw new Error(`Polling ${label} timed out after ${maxAttempts} attempts`);
}

// ═══════════════════════════════════════════════════════════════
// IDEMPOTENT "ENSURE" HELPERS
// Mirrors the Seeder.App pattern: check if exists → create if not
// ═══════════════════════════════════════════════════════════════

/**
 * Ensures a user exists. Registers if not present. Returns authenticated context.
 * Mirrors UserSeeder.EnsureUserExistsAsync
 */
export async function ensureUserExists(
  requestFactory: APIRequestContext,
  firstName: string,
  lastName: string,
  email: string,
  password: string
): Promise<APIRequestContext> {
  // Idempotency: try login first
  try {
    return await loginApi(requestFactory, email, password);
  } catch {
    // User doesn't exist — register
    return await registerApi(requestFactory, firstName, lastName, email, password);
  }
}

/**
 * Ensures a store exists and is verified. Creates + verifies if not present.
 * Mirrors StoreSeeder.EnsureStoreExistsAsync
 */
export async function ensureStoreExists(
  sellerApi: APIRequestContext,
  adminApi: APIRequestContext,
  sellerId: string,
  name: string,
  description: string
): Promise<StoreResult> {
  // Check if store already exists
  const existing = await getStoreBySellerId(sellerApi, sellerId);
  if (existing) {
    // Verify if not already verified
    if (existing.verificationStatus !== 'Verified') {
      try {
        await verifyStore(adminApi, existing.id, true);
        existing.verificationStatus = 'Verified';
      } catch {
        // 409 if already verified
      }
    }
    return existing;
  }

  // Create store
  const store = await createStore(sellerApi, sellerId, name, description);

  // Verify via admin
  try {
    await verifyStore(adminApi, store.id, true);
    store.verificationStatus = 'Verified';
  } catch {
    // May already be verified
  }

  return store;
}

/**
 * Ensures a category exists. Creates if not present. Returns the category.
 * Mirrors CategorySeeder.EnsureCategoryExistsAsync
 */
export async function ensureCategoryExists(
  adminApi: APIRequestContext,
  name: string,
  description: string
): Promise<CategoryResult> {
  const existing = await getCategories(adminApi);
  const match = existing.find(c => c.name === name);
  if (match) return match;

  return createCategory(adminApi, name, description);
}

/**
 * Ensures a product exists with inventory. Creates product + inventory if not present.
 * Mirrors ProductSeeder + InventorySeeder patterns.
 */
export async function ensureProductExists(
  sellerApi: APIRequestContext,
  product: {
    name: string;
    description: string;
    sku: string;
    price: number;
    currency: string;
    categoryId: string;
    storeId: string;
    tags?: string[];
    imageUrl?: string;
  },
  initialStock: number
): Promise<ProductResult> {
  // Check if product already exists by SKU
  const existing = await getProductBySku(sellerApi, product.sku);
  if (existing) {
    return existing;
  }

  // Create product
  const created = await createProduct(sellerApi, product);

  // Activate the product (ignore if endpoint doesn't exist)
  try {
    await activateProduct(sellerApi, created.id);
  } catch {
    // Activation endpoint may not exist
  }

  // Set inventory stock
  if (initialStock > 0) {
    try {
      await createInventoryItem(sellerApi, product.sku, initialStock);
    } catch {
      // 409 if already exists — that's fine
    }
  }

  return created;
}

/**
 * Full checkout flow: add items to cart → checkout → poll for completion.
 * Mirrors OrderFlowSeeder.RunOrderFlowAsync
 */
export async function runCheckoutFlow(
  buyerApi: APIRequestContext,
  items: Array<{ sku: string; quantity: number; price: number; shopId?: string }>,
  address: {
    addressLine1: string;
    city: string;
    state: string;
    postalCode: string;
    country: string;
  },
  pollOptions?: PollOptions
): Promise<{ correlationId: string; finalOrder: OrderResult | null }> {
  // 1. Add items to cart
  for (const item of items) {
    await addToCart(buyerApi, item.sku, item.quantity, item.price, item.shopId);
  }

  // 2. Checkout
  const checkoutResponse = await buyerApi.post('/api/cart/checkout', { data: address });
  if (!checkoutResponse.ok()) {
    const err = await checkoutResponse.text();
    throw new Error(`Checkout failed: ${checkoutResponse.status()} ${err}`);
  }

  const checkoutResult = await checkoutResponse.json();
  const correlationId = checkoutResult.correlationId;
  if (!correlationId) {
    throw new Error('Checkout returned no correlationId');
  }

  // 3. Poll for terminal order status
  const terminalStatuses = ['Completed', 'Cancelled', 'Faulted'];
  let finalOrder: OrderResult | null = null;

  try {
    finalOrder = await poll(
      async () => {
        const order = await getOrder(buyerApi, correlationId);
        if (order && terminalStatuses.includes(order.statusName)) {
          return order;
        }
        return null;
      },
      { maxAttempts: 30, delayMs: 2000, label: 'order completion', ...pollOptions }
    );
  } catch {
    // Order didn't reach terminal state in time — return what we have
    finalOrder = await getOrder(buyerApi, correlationId);
  }

  return { correlationId, finalOrder };
}

// ═══════════════════════════════════════════════════════════════
// UI-LEVEL HELPERS
// For tests that need an authenticated browser page, not just API.
// ═══════════════════════════════════════════════════════════════

export interface AuthenticatedPage {
  page: Page;
  context: BrowserContext;
  email: string;
  password: string;
}

/**
 * Registers a fresh user via UI, handles auto-login redirect,
 * and returns an authenticated Page ready for testing.
 *
 * Eliminates the duplicated register-login-beforeEach pattern
 * found in 8+ test files.
 *
 * Mirrors UserSeeder.EnsureUserExistsAsync but for browser context.
 */
export async function ensureAuthenticatedPage(
  browser: Browser,
  options: {
    firstName?: string;
    lastName?: string;
    role?: 'buyer' | 'seller';
  } = {}
): Promise<AuthenticatedPage> {
  const { firstName = 'E2E', lastName = 'User' } = options;
  const uniqueId = Math.random().toString(36).substring(7);
  const email = `e2e-${uniqueId}@test.com`;
  const password = 'P@ssw0rd123!';

  const context = await browser.newContext();
  const page = await context.newPage();

  // Register
  await page.goto('/auth/register');
  await page.waitForLoadState('domcontentloaded');
  await page.getByTestId('first-name-input').fill(firstName);
  await page.getByTestId('last-name-input').fill(lastName);
  await page.getByTestId('email-input').fill(email);
  await page.getByTestId('password-input').fill(password);
  await page.getByTestId('register-submit-btn').click();

  // Wait for redirect — either to catalog (auto-login) or login page
  await page.waitForURL(/\/(catalog|auth\/login)$/, { timeout: 15000 });

  // If redirected to login, perform login
  if (page.url().includes('/auth/login')) {
    await page.getByTestId('email-input').fill(email);
    await page.getByTestId('password-input').fill(password);
    await page.getByTestId('login-submit-btn').click();
    await page.waitForURL(/\/catalog/, { timeout: 15000 });
  }

  return { page, context, email, password };
}

/**
 * Ensures a user is authenticated via API, copies cookies to a browser
 * context, and returns the ready page.
 *
 * Faster than ensureAuthenticatedPage — skips UI registration entirely.
 * Use when the test doesn't need to verify the registration flow itself.
 */
export async function ensureAuthenticatedPageViaApi(
  browser: Browser,
  requestFactory: APIRequestContext,
  options: {
    firstName?: string;
    lastName?: string;
    email?: string;
    password?: string;
    role?: 'Buyer' | 'Seller' | 'Admin';
  } = {}
): Promise<AuthenticatedPage & { api: APIRequestContext }> {
  const uniqueId = Math.random().toString(36).substring(7);
  const email = options.email ?? `e2e-${uniqueId}@test.com`;
  const password = options.password ?? 'P@ssw0rd123!';
  const firstName = options.firstName ?? 'E2E';
  const lastName = options.lastName ?? 'User';
  const role = options.role ?? 'Buyer';

  // Register/login via API (fast, no UI)
  let api = await ensureUserExists(requestFactory, firstName, lastName, email, password);

  // Promote to requested role if not Buyer, then re-login for fresh JWT
  if (role !== 'Buyer') {
    const user = await getCurrentUser(api);
    const adminApi = await loginApi(requestFactory, 'admin@marketplace.com', 'P@ssw0rd123!');
    try {
      if (role === 'Seller') {
        await promoteToSeller(adminApi, user.id);
      }
    } catch {
      // Already promoted
    }
    await adminApi.dispose();

    // Re-login to get JWT with updated role
    await api.dispose();
    api = await loginApi(requestFactory, email, password);
  }

  const storageState = await api.storageState();

  // Create browser context with the auth cookies
  const context = await browser.newContext();
  await context.addCookies(storageState.cookies);

  const page = await context.newPage();
  // Navigate to establish the session
  await page.goto('/catalog');
  await page.waitForLoadState('domcontentloaded');

  return { page, context, email, password, api };
}

// ═══════════════════════════════════════════════════════════════
// HIGH-LEVEL TEST DATA BUILDER
// Mirrors the full Seeder.App pipeline in a single call.
// ═══════════════════════════════════════════════════════════════

export interface TestDataSetup {
  /** Authenticated API context for the buyer */
  buyerApi: APIRequestContext;
  /** Authenticated API context for the seller */
  sellerApi: APIRequestContext;
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
  } catch {
    // Already a seller
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
    sellerApiFresh,
    adminApi,
    store,
    products,
    categories: [category],
    buyer,
    seller,
  };
}
