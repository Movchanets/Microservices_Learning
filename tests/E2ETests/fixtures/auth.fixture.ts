// Auth fixture with storageState-based pre-authenticated browser contexts
// and role-based API request contexts. Eliminates duplicated login boilerplate.
//
// Each worker gets its own storage state files (worker-indexed) to prevent
// race conditions when running tests in parallel.

import { APIRequestContext, BrowserContext } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { loginApi, registerApi, getCurrentUser, type BffUser } from '../utils/api-helpers';
import * as users from '../data/users.json';
import { test as pageTest } from './test-base';

const BASE_URL = process.env.BASE_URL || 'http://localhost:4200';
const STORAGE_DIR = path.join(os.tmpdir(), 'playwright-auth');

// ── Helpers ───────────────────────────────────────────────

async function ensureStorageDir(): Promise<void> {
  await fs.promises.mkdir(STORAGE_DIR, { recursive: true });
}

/**
 * Logs in via BFF API, saves storageState to a worker-indexed temp file.
 * Each parallel worker gets its own file to avoid race conditions.
 */
async function loginAndSaveState(
  requestFactory: APIRequestContext,
  email: string,
  password: string,
  label: string,
  workerIndex: number
): Promise<{ statePath: string; api: APIRequestContext }> {
  await ensureStorageDir();
  // Worker-indexed filename prevents cross-worker race conditions
  const statePath = path.join(STORAGE_DIR, `${label}-w${workerIndex}.json`);

  // Reuse cached state if fresh (< 30 min old) and valid
  try {
    const stat = await fs.promises.stat(statePath);
    const ageMinutes = (Date.now() - stat.mtimeMs) / 60_000;
    if (ageMinutes < 30 && stat.size > 10) {
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

// Password used for all auto-generated isolated buyers
const ISOLATED_BUYER_PASSWORD = 'P@ssw0rd123!';

/**
 * Registers (or falls back to login) a deterministic per-worker buyer,
 * saves storageState to a worker-indexed temp file with 30-min cache.
 */
async function registerAndSaveState(
  requestFactory: APIRequestContext,
  workerIndex: number
): Promise<{ statePath: string; api: APIRequestContext }> {
  await ensureStorageDir();
  const email = `buyer+w${workerIndex}@marketplace.com`;
  const statePath = path.join(STORAGE_DIR, `isolated-buyer-w${workerIndex}.json`);

  // Reuse cached state if fresh (< 30 min old) and valid
  try {
    const stat = await fs.promises.stat(statePath);
    const ageMinutes = (Date.now() - stat.mtimeMs) / 60_000;
    if (ageMinutes < 30 && stat.size > 10) {
      const content = await fs.promises.readFile(statePath, 'utf-8');
      const parsed = JSON.parse(content);
      if (parsed.cookies && parsed.cookies.length > 0) {
        const api = await loginApi(requestFactory, email, ISOLATED_BUYER_PASSWORD);
        return { statePath, api };
      }
    }
  } catch {
    // File doesn't exist or is invalid — proceed with registration
  }

  const api = await registerApi(
    requestFactory,
    'Buyer',
    `Worker${workerIndex}`,
    email,
    ISOLATED_BUYER_PASSWORD
  );
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
  /** Isolated per-worker buyer API context (unique email per worker) */
  isolatedBuyerApi: APIRequestContext;
  /** Isolated per-worker buyer user info */
  isolatedBuyerUser: BffUser;
  /** Isolated per-worker browser context pre-authenticated as buyer */
  isolatedBuyerContext: BrowserContext;
};

// ── Auth Test (extends pageTest with auth fixtures) ───────

export const authTest = pageTest.extend<AuthFixtures>({
  // ── API Contexts ────────────────────────────────────────

  buyerApi: async ({ playwright }, use, testInfo) => {
    const { api } = await loginAndSaveState(
      playwright.request,
      users.buyerUser.email,
      users.buyerUser.password,
      'buyer',
      testInfo.workerIndex
    );
    await use(api);
    await api.dispose();
  },

  sellerApi: async ({ playwright }, use, testInfo) => {
    const { api } = await loginAndSaveState(
      playwright.request,
      users.sellerUser.email,
      users.sellerUser.password,
      'seller',
      testInfo.workerIndex
    );
    await use(api);
    await api.dispose();
  },

  adminApi: async ({ playwright }, use, testInfo) => {
    const { api } = await loginAndSaveState(
      playwright.request,
      users.adminUser.email,
      users.adminUser.password,
      'admin',
      testInfo.workerIndex
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

  buyerContext: async ({ browser, playwright }, use, testInfo) => {
    const { statePath } = await loginAndSaveState(
      playwright.request,
      users.buyerUser.email,
      users.buyerUser.password,
      'buyer',
      testInfo.workerIndex
    );
    const context = await browser.newContext({ storageState: statePath });
    await use(context);
    await context.close();
  },

  sellerContext: async ({ browser, playwright }, use, testInfo) => {
    const { statePath } = await loginAndSaveState(
      playwright.request,
      users.sellerUser.email,
      users.sellerUser.password,
      'seller',
      testInfo.workerIndex
    );
    const context = await browser.newContext({ storageState: statePath });
    await use(context);
    await context.close();
  },

  adminContext: async ({ browser, playwright }, use, testInfo) => {
    const { statePath } = await loginAndSaveState(
      playwright.request,
      users.adminUser.email,
      users.adminUser.password,
      'admin',
      testInfo.workerIndex
    );
    const context = await browser.newContext({ storageState: statePath });
    await use(context);
    await context.close();
  },

  // ── Isolated Buyer (per-worker unique account) ─────────

  isolatedBuyerApi: async ({ playwright }, use, testInfo) => {
    const { api } = await registerAndSaveState(
      playwright.request,
      testInfo.workerIndex
    );
    await use(api);
    await api.dispose();
  },

  isolatedBuyerUser: async ({ isolatedBuyerApi }, use) => {
    const user = await getCurrentUser(isolatedBuyerApi);
    await use(user);
  },

  isolatedBuyerContext: async ({ browser, playwright }, use, testInfo) => {
    const { statePath } = await registerAndSaveState(
      playwright.request,
      testInfo.workerIndex
    );
    const context = await browser.newContext({ storageState: statePath });
    await use(context);
    await context.close();
  },
});

export { expect } from '@playwright/test';
