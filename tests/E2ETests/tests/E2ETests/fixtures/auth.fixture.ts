// Auth fixture with storageState-based pre-authenticated browser contexts
// and role-based API request contexts. Eliminates duplicated login boilerplate.
//
// Usage:
//   import { authTest as test, expect } from '../../fixtures/auth.fixture';
//
//   test('seller can access dashboard', async ({ sellerContext }) => {
//     const page = await sellerContext.newPage();
//     await page.goto('/seller');
//   });

import { test as base, APIRequestContext, BrowserContext } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { loginApi, getCurrentUser, type BffUser } from '../utils/api-helpers';
import * as users from '../data/users.json';

const STORAGE_DIR = path.join(os.tmpdir(), 'playwright-auth');

async function ensureStorageDir(): Promise<void> {
  await fs.promises.mkdir(STORAGE_DIR, { recursive: true }).catch(() => {});
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

  const api = await loginApi(requestFactory, email, password);
  const state = await api.storageState();
  await fs.promises.writeFile(statePath, JSON.stringify(state));

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

// ── Auth Test ─────────────────────────────────────────────

export const authTest = base.extend<AuthFixtures>({
  // ── API Contexts ────────────────────────────────────────

  buyerApi: async ({ playwright }, use) => {
    const { api } = await loginAndSaveState(
      playwright.request, users.buyerUser.email, users.buyerUser.password, 'buyer'
    );
    await use(api);
    await api.dispose();
  },

  sellerApi: async ({ playwright }, use) => {
    const { api } = await loginAndSaveState(
      playwright.request, users.sellerUser.email, users.sellerUser.password, 'seller'
    );
    await use(api);
    await api.dispose();
  },

  adminApi: async ({ playwright }, use) => {
    const { api } = await loginAndSaveState(
      playwright.request, users.adminUser.email, users.adminUser.password, 'admin'
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
      playwright.request, users.buyerUser.email, users.buyerUser.password, 'buyer'
    );
    const context = await browser.newContext({ storageState: statePath });
    await use(context);
    await context.close();
  },

  sellerContext: async ({ browser, playwright }, use) => {
    const { statePath } = await loginAndSaveState(
      playwright.request, users.sellerUser.email, users.sellerUser.password, 'seller'
    );
    const context = await browser.newContext({ storageState: statePath });
    await use(context);
    await context.close();
  },

  adminContext: async ({ browser, playwright }, use) => {
    const { statePath } = await loginAndSaveState(
      playwright.request, users.adminUser.email, users.adminUser.password, 'admin'
    );
    const context = await browser.newContext({ storageState: statePath });
    await use(context);
    await context.close();
  },
});

export { expect } from '@playwright/test';
