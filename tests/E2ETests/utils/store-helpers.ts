/**
 * Store Management API helpers.
 */

import { APIRequestContext, Page, expect } from '@playwright/test';
import type { StoreResult } from './types';
import { TIMEOUTS } from './constants';

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
    const body = await response.text();
    // Handle STORE_DUPLICATE: store already exists for this seller — fetch and return it
    if (response.status() === 400 && body.includes('STORE_DUPLICATE')) {
      const existing = await getStoreBySellerId(api, sellerId);
      if (existing) return existing;
    }
    throw new Error(`Create store failed: ${response.status()} ${body}`);
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
  // 409 = already verified — idempotent, ignore
  if (!response.ok() && response.status() !== 409) {
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

// ── Idempotent Store Helper ──

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
 * Ensure the seller has a store via the UI (Create Your Store form).
 *
 * Navigates to /seller, checks if the "Create Your Store" form is visible,
 * and fills it out if needed. Use this in tests that require a store to exist
 * but can't rely on API-only creation (e.g., UI flow tests).
 */
export async function ensureSellerStoreViaUi(
  page: Page,
  storeName: string,
  storeDescription: string
): Promise<void> {
  await page.goto('/seller');
  await page.waitForLoadState('domcontentloaded');

  const createStoreHeading = page.getByRole('heading', { name: 'Create Your Store' });
  const hasCreateForm = await createStoreHeading
    .isVisible({ timeout: TIMEOUTS.quick })
    .catch(() => false);

  if (!hasCreateForm) return; // Store already exists

  const storeNameInput = page.getByTestId('store-name-input');
  const storeDescInput = page.getByPlaceholder('Tell customers what your store is about...');
  const createStoreBtn = page.getByRole('button', { name: 'Create Store' });

  await expect(storeNameInput).toBeVisible({ timeout: TIMEOUTS.quick });

  await storeNameInput.click({ clickCount: 3 });
  await storeNameInput.pressSequentially(storeName, { delay: 20 });
  await storeDescInput.click({ clickCount: 3 });
  await storeDescInput.pressSequentially(storeDescription, { delay: 20 });

  // Wait for Angular form reactivity — button becomes enabled when form is valid
  await expect(createStoreBtn).toBeEnabled({ timeout: TIMEOUTS.element });
  await createStoreBtn.click();

  // Wait for store creation to complete — heading disappears when store exists
  await createStoreHeading.waitFor({ state: 'hidden', timeout: TIMEOUTS.api });
}
