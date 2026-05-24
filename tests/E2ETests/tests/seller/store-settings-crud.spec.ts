import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { StoreSettingsPage } from '../../pages/store-settings.page';

test.describe('Seller: Store Settings CRUD', () => {

  test('should display store settings page for seller', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    const settingsPage = new StoreSettingsPage(page);

    await settingsPage.goto();

    await expect(page.getByRole('heading', { name: /store settings/i })).toBeVisible();
    await page.close();
  });

  test('should show store name and description fields', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    const settingsPage = new StoreSettingsPage(page);

    await settingsPage.goto();

    await expect(settingsPage.storeNameInput).toBeVisible();
    await expect(settingsPage.storeDescInput).toBeVisible();
    await page.close();
  });

  test('should update store name and description', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    const settingsPage = new StoreSettingsPage(page);

    await settingsPage.goto();
    await page.waitForLoadState('domcontentloaded');

    const newName = `Updated Store ${Date.now()}`;
    const newDesc = 'Updated description from E2E test';

    await settingsPage.updateStore(newName, newDesc);
    await page.waitForLoadState('domcontentloaded');

    // Should show success feedback (toast or inline message)
    const successMsg = page.getByText(/success|saved|updated/i).first();
    await expect(successMsg).toBeVisible({ timeout: 10000 });
    await page.close();
  });

  test('should redirect unauthenticated from store settings', async ({ browser }) => {
    const page = await browser.newPage();
    await page.goto('/seller/settings');
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/auth\/login/);
    await page.close();
  });
});
