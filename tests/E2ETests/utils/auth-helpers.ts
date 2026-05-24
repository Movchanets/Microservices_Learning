/**
 * Authentication and identity API helpers.
 * Handles login, registration, user management, and browser-context auth.
 */

import { APIRequestContext, Browser, BrowserContext, Page } from '@playwright/test';
import type { BffUser } from './types';
import { ensureStoreExists } from './store-helpers';

const BASE_URL = process.env.BASE_URL || 'http://localhost:4200';

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
  const tempCtx = await (requestFactory as any).newContext({ baseURL: BASE_URL }) as APIRequestContext;

  const loginResponse = await tempCtx.post(`${BASE_URL}/bff/auth/login`, {
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
    baseURL: BASE_URL,
    storageState: state,
    extraHTTPHeaders: {
      'X-XSRF-TOKEN': xsrfToken,
    },
  }) as APIRequestContext;

  return context;
}

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
  const tempCtx = await (requestFactory as any).newContext({ baseURL: BASE_URL }) as APIRequestContext;

  const registerResponse = await tempCtx.post(`${BASE_URL}/bff/auth/register`, {
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
    baseURL: BASE_URL,
    storageState: state,
    extraHTTPHeaders: {
      'X-XSRF-TOKEN': xsrfToken,
    },
  }) as APIRequestContext;

  return context;
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

// ── Idempotent User Helper ──

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

// ── UI-Level Helpers ──

export interface AuthenticatedPage {
  page: Page;
  context: BrowserContext;
  email: string;
  password: string;
}

/**
 * Registers a fresh user via UI, handles auto-login redirect,
 * and returns an authenticated Page ready for testing.
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

    // Re-login to get JWT with updated role
    await api.dispose();
    api = await loginApi(requestFactory, email, password);

    // Create a store for sellers so dashboard tabs appear
    if (role === 'Seller') {
      try {
        const user2 = await getCurrentUser(api);
        await ensureStoreExists(api, adminApi, user2.id, `E2E Store ${uniqueId}`, 'E2E test store');
      } catch (e) {
        console.warn(`[ensureAuthenticatedPageViaApi] Store creation warning: ${e}`);
      }
    }

    await adminApi.dispose();
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
