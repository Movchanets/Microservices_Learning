import { test as setup } from '@playwright/test';
import * as path from 'path';
import * as fs from 'fs';
import { ensureUserExists, getCurrentUser, promoteToSeller, loginApi } from '../utils/api-helpers';
import { ensureStoreExists } from '../utils/store-helpers';
import * as users from '../data/users.json';

const AUTH_DIR = path.join(__dirname, '../playwright/.auth');

/**
 * StorageState auth setup -- ensures users exist, logs in, saves cookies.
 * Runs before all test projects. Each role gets its own state file.
 *
 * Pattern: ensure user exists (register if needed) -> login -> save storageState.
 * For seller: also promotes to seller role and creates a store.
 */

async function loginAndSave(
  request: any,
  email: string,
  password: string,
  firstName: string,
  lastName: string,
  role: string,
): Promise<import('@playwright/test').APIRequestContext> {
  const stateDir = path.join(AUTH_DIR, role);
  await fs.promises.mkdir(stateDir, { recursive: true });
  const statePath = path.join(stateDir, 'state.json');

  // Ensure user exists (register if not present), then login
  const api = await ensureUserExists(request, firstName, lastName, email, password);
  const user = await getCurrentUser(api);
  console.log(`[auth.setup] Logged in as ${role}: ${user.email} (${user.id})`);

  const state = await api.storageState();
  await fs.promises.writeFile(statePath, JSON.stringify(state, null, 2));
  return api;
}

// Admin first -- needed to promote seller and verify store
setup('authenticate admin', async ({ playwright }) => {
  await loginAndSave(
    playwright.request,
    users.adminUser.email,
    users.adminUser.password,
    'Admin',
    'User',
    'admin',
  );
});

// Buyer -- simple login
setup('authenticate buyer', async ({ playwright }) => {
  await loginAndSave(
    playwright.request,
    users.buyerUser.email,
    users.buyerUser.password,
    'Test',
    'Buyer',
    'buyer',
  );
});

// Seller -- promote to seller role, create store, then save state
setup('authenticate seller', async ({ playwright }) => {
  const sellerApi = await loginAndSave(
    playwright.request,
    users.sellerUser.email,
    users.sellerUser.password,
    'Tech',
    'Store',
    'seller',
  );

  // Login as admin FIRST — needed to promote seller (endpoint requires Admin role)
  const adminApi = await loginApi(playwright.request, users.adminUser.email, users.adminUser.password);

  // Promote to seller role via admin context
  const seller = await getCurrentUser(sellerApi);
  try {
    await promoteToSeller(adminApi, seller.id);
    console.log(`[auth.setup] Promoted ${seller.email} to Seller`);
  } catch {
    // Already seller (409)
  }

  // Re-login to get JWT with Seller role claim
  await sellerApi.dispose();
  const freshSellerApi = await loginApi(playwright.request, users.sellerUser.email, users.sellerUser.password);

  // Ensure store exists and is verified
  const _store = await ensureStoreExists(
    freshSellerApi,
    adminApi,
    seller.id,
    'E2E Tech Store',
    'Automated test store for E2E tests',
  );
  console.log(`[auth.setup] Store ensured for seller: ${seller.email}`);

  // Re-save seller state with updated JWT
  const stateDir = path.join(AUTH_DIR, 'seller');
  const statePath = path.join(stateDir, 'state.json');
  const state = await freshSellerApi.storageState();
  await fs.promises.writeFile(statePath, JSON.stringify(state, null, 2));

  await freshSellerApi.dispose();
  await adminApi.dispose();
});
