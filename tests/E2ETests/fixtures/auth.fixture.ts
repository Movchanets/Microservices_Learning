// Auth fixture with storageState-based pre-authenticated browser contexts
// and role-based API request contexts. Eliminates duplicated login boilerplate.
//
// Usage:
//   import { authTest as test, expect } from '../../fixtures/auth.fixture';
//
//   test('seller can access dashboard', async ({ page, sellerAuth }) => {
//     // page is already authenticated as seller
//     await page.goto('/seller');
//   });
//
//   test('admin can verify store', async ({ page, adminAuth, adminApi }) => {
//     // page is authenticated as admin
//     // adminApi is an APIRequestContext for programmatic setup
//     await page.goto('/admin');
//   });

import { test as base, APIRequestContext, BrowserContext } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { loginApi, getCurrentUser, type BffUser } from '../utils/api-helpers';
import * as users from '../data/users.json';
import { test as pageTest } from './test-base';

const BASE_URL = process.env.BASE_URL || 'http://localhost:4200';
const STORAGE_DIR = path.join(os.tmpdir(), 'playwright-auth');

// ── Helpers ───────────────────────────────────────────────

async function ensureStorageDir(): Promise<void> {
  await fs.promises.mkdir(STORAGE_DIR, { recursive: true });
}

/**
 * Logs in via BFF API, saves storageState to a temp file.
 * Returns the file path for use with browser context storageState.
 */
async function loginAndSaveState(
  requestFactory: APIRequestContext,
  email: string,
  password: string,
  label: string
): Promise<{ statePath: string; api: APIRequestContext }> {
  await ensureStorageDir();
  const statePath = path.join(STORAGE_DIR, `${label}.json`);

  // Reuse cached state if fresh (< 30 min old) and valid
  try {
    const stat = await fs.promises.stat(statePath);
    const ageMinutes = (Date.now() - stat.mtimeMs) / 60_000;
    if (ageMinutes < 30 && stat.size > 10) {
      // Validate the file is valid JSON with cookies
      const content = await fs.promises.readFile(statePath, 'utf-8');
      const parsed = JSON.parse(content);
      if (parsed.cookies && parsed.cookies.length > 0) {
        const api = await loginApi(requestFactory, email, password);
        return { statePath, api };
      }
    }
  } catch {
    // File doesn't exist or is invalid — proceed with login
  }

  const api = await loginApi(requestFactory, email, password);
  const state = await api.storageState();
  await fs.promises.writeFile(statePath, JSON.stringify(state, null, 2));

  return { statePath, api };
}

// ── Fixture Types ─────────────────────────────────────────

export type AuthFixtures = {
  /** Authenticated API context for buyer */
  buyerApi: APIRequestContext;
  /** Authenticated API context for seller */
  sellerApi: APIRequestContext;
  /** Authenticated API context for admin */
  adminApi: APIRequestContext;
  /** Authenticated buyer user info */
  buyerUser: BffUser;
  /** Authenticated seller user info */
  sellerUser: BffUser;
  /** Authenticated admin user info */
  adminUser: BffUser;
  /** Browser context pre-authenticated as buyer */
  buyerContext: BrowserContext;
  /** Browser context pre-authenticated as seller */
  sellerContext: BrowserContext;
  /** Browser context pre-authenticated as admin */
  adminContext: BrowserContext;
};

// ── Auth Test (extends pageTest with auth fixtures) ───────

export const authTest = pageTest.extend<AuthFixtures>({
  // ── API Contexts ────────────────────────────────────────

  buyerApi: async ({ playwright }, use) => {
    const { api } = await loginAndSaveState(
      playwright.request,
      users.buyerUser.email,
      users.buyerUser.password,
      'buyer'
    );
    await use(api);
    await api.dispose();
  },

  sellerApi: async ({ playwright }, use) => {
    const { api } = await loginAndSaveState(
      playwright.request,
      users.sellerUser.email,
      users.sellerUser.password,
      'seller'
    );
    await use(api);
    await api.dispose();
  },

  adminApi: async ({ playwright }, use) => {
    const { api } = await loginAndSaveState(
      playwright.request,
      users.adminUser.email,
      users.adminUser.password,
      'admin'
    );
    await use(api);
    await api.dispose();
  },

  // ── User Info ───────────────────────────────────────────

  buyerUser: async ({ buyerApi }, use) => {
    const user = await getCurrentUser(buyerApi);
    await use(user);
  },

  sellerUser: async ({ sellerApi }, use) => {
    const user = await getCurrentUser(sellerApi);
    await use(user);
  },

  adminUser: async ({ adminApi }, use) => {
    const user = await getCurrentUser(adminApi);
    await use(user);
  },

  // ── Pre-authenticated Browser Contexts ──────────────────

  buyerContext: async ({ browser, playwright }, use) => {
    const { statePath } = await loginAndSaveState(
      playwright.request,
      users.buyerUser.email,
      users.buyerUser.password,
      'buyer'
    );
    const context = await browser.newContext({ storageState: statePath });
    await use(context);
    await context.close();
  },

  sellerContext: async ({ browser, playwright }, use) => {
    const { statePath } = await loginAndSaveState(
      playwright.request,
      users.sellerUser.email,
      users.sellerUser.password,
      'seller'
    );
    const context = await browser.newContext({ storageState: statePath });
    await use(context);
    await context.close();
  },

  adminContext: async ({ browser, playwright }, use) => {
    const { statePath } = await loginAndSaveState(
      playwright.request,
      users.adminUser.email,
      users.adminUser.password,
      'admin'
    );
    const context = await browser.newContext({ storageState: statePath });
    await use(context);
    await context.close();
  },
});

export { expect } from '@playwright/test';
