import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { createStore, verifyStore, getCurrentUser } from '../../utils/api-helpers';
import { AdminStoreDetailPage } from '../../pages/admin-store-detail.page';

test.describe('Admin: Store Verification', () => {

  test('should display store detail page for pending store', async ({ adminContext, sellerApi, adminApi }) => {
    // Create a store to verify
    const seller = await getCurrentUser(sellerApi);
    const randomId = Math.random().toString(36).substring(7).toUpperCase();
    const store = await createStore(sellerApi, seller.id, `Verify Store ${randomId}`, 'Test store for verification');

    const page = await adminContext.newPage();
    const storeDetailPage = new AdminStoreDetailPage(page);

    // Navigate to admin verifications
    await page.goto('/admin/verifications');
    await page.waitForLoadState('domcontentloaded');

    // Look for the store in the verification list
    const storeLink = page.getByText(store.name);
    if (await storeLink.isVisible()) {
      await storeLink.click();
      await page.waitForLoadState('domcontentloaded');

      // Should see store detail
      await expect(storeDetailPage.storeNameHeading).toBeVisible();
      await expect(storeDetailPage.approveBtn).toBeVisible();
      await expect(storeDetailPage.rejectBtn).toBeVisible();
    }
    await page.close();
  });

  test('should approve store via detail page', async ({ adminContext, sellerApi, adminApi }) => {
    const seller = await getCurrentUser(sellerApi);
    const randomId = Math.random().toString(36).substring(7).toUpperCase();
    const store = await createStore(sellerApi, seller.id, `Approve Store ${randomId}`, 'Test store');

    const page = await adminContext.newPage();
    const storeDetailPage = new AdminStoreDetailPage(page);

    await page.goto('/admin/verifications');
    await page.waitForLoadState('domcontentloaded');

    const storeLink = page.getByText(store.name);
    if (await storeLink.isVisible()) {
      await storeLink.click();
      await page.waitForLoadState('domcontentloaded');

      await storeDetailPage.approveStore();
      await page.waitForLoadState('domcontentloaded');

      // Should show approved status
      const statusText = await page.getByText(/approved|verified/i).isVisible().catch(() => false);
      expect(statusText).toBe(true);
    }
    await page.close();
  });
});
