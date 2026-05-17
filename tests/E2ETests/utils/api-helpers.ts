import { APIRequestContext } from '@playwright/test';

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
  const baseUrl = 'http://localhost:4200';

  const tempCtx = await (requestFactory as any).newContext({ baseURL: baseUrl }) as APIRequestContext;

  const loginResponse = await tempCtx.post(`${baseUrl}/bff/auth/login`, {
    data: { email, password },
  });

  if (!loginResponse.ok()) {
    await tempCtx.dispose();
    throw new Error(
      `Login failed for ${email}: ${loginResponse.status()} ${await loginResponse.text()}`
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

export async function getCategories(
  api: APIRequestContext
): Promise<CategoryResult[]> {
  const response = await api.get('/api/catalog/categories');
  if (!response.ok()) {
    throw new Error(`Get categories failed: ${response.status()} ${await response.text()}`);
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
  const baseUrl = 'http://localhost:4200';

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
  price: number
): Promise<void> {
  // Get current cart to preserve existing items
  const cartResponse = await api.get('/api/cart');
  let existingItems: Array<{ sku: string; quantity: number; price: number }> = [];

  if (cartResponse.ok()) {
    const cart = await cartResponse.json();
    existingItems = (cart.items || []).map((i: any) => ({
      sku: i.sku,
      quantity: i.quantity,
      price: i.unitPrice ?? i.price ?? 0,
    }));
  }

  // Add or update the new item
  const existingIndex = existingItems.findIndex((i) => i.sku === sku);
  if (existingIndex >= 0) {
    existingItems[existingIndex].quantity += quantity;
  } else {
    existingItems.push({ sku, quantity, price });
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
