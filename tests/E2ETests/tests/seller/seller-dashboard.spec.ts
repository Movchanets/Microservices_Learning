import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { TIMEOUTS } from '../../utils/constants';

test.describe('Seller: Dashboard', () => {

  test('should show seller dashboard for seller users', async ({ sellerDashboardPage }) => {
    await test.step('Navigate to seller page', async () => {
      await sellerDashboardPage.goto();
      await sellerDashboardPage.waitForPageLoad();
    });

    await test.step('Wait for Angular to stabilize', async () => {
      const page = sellerDashboardPage.page;
      const spinner = page.locator('.animate-spin');

      // Wait for all JS bundles loaded so Angular can bootstrap
      await page.waitForLoadState('load');

      // Angular hydrates → loadSettings() → spinner appears (briefly).
      // If spinner never shows, Angular hydrated + API resolved instantly.
      const spinnerAppeared = await spinner
        .waitFor({ state: 'visible', timeout: 3000 })
        .then(() => true)
        .catch(() => false);

      if (spinnerAppeared) {
        // Wait for API to resolve — spinner disappears when loadSettings() completes
        await spinner.waitFor({ state: 'hidden', timeout: TIMEOUTS.api });
      }
    });

    await test.step('Create store if needed', async () => {
      const hasCreateForm = await sellerDashboardPage.createStoreHeading.isVisible();
      if (hasCreateForm) {
        await sellerDashboardPage.createStore('E2E Test Store', 'Automated test store for E2E tests');
      }
    });

    await test.step('Verify dashboard heading and navigation tabs', async () => {
      await expect(sellerDashboardPage.pageHeading).toBeVisible();
      await expect(sellerDashboardPage.productsTab).toBeVisible();
      await expect(sellerDashboardPage.ordersLink).toBeVisible();
      await expect(sellerDashboardPage.settingsTab).toBeVisible();
    });
  });

  test('should navigate to seller products', async ({ sellerProductsPage }) => {
    await test.step('Navigate to seller products page', async () => {
      await sellerProductsPage.goto();
      await sellerProductsPage.waitForPageLoad();
    });

    await test.step('Verify products heading is visible', async () => {
      const heading = sellerProductsPage.page.getByRole('heading').filter({ hasText: /products/i });
      await expect(heading.first()).toBeVisible({ timeout: TIMEOUTS.element });
    });
  });

  test('should navigate to store settings', async ({ storeSettingsPage }) => {
    await test.step('Navigate to store settings page', async () => {
      await storeSettingsPage.goto();
      await storeSettingsPage.waitForPageLoad();
    });

    await test.step('Verify store settings heading', async () => {
      await expect(storeSettingsPage.page.getByRole('heading').filter({ hasText: /settings/i }).first())
        .toBeVisible({ timeout: TIMEOUTS.element });
    });
  });
});
