import { authTest as test, expect } from '../fixtures/auth.fixture';

test.describe('User Profile Hub', () => {

  test('should display profile hub with sidebar navigation', async ({ profileHubPage }) => {
    await test.step('Navigate to profile hub', async () => {
      await profileHubPage.goto();
      await profileHubPage.waitForPageLoad();
    });

    await test.step('Verify page heading is visible', async () => {
      await expect(profileHubPage.pageHeading).toBeVisible();
    });
  });

  test('should navigate between profile tabs', async ({ profileHubPage, page }) => {
    await test.step('Navigate to profile hub', async () => {
      await profileHubPage.goto();
      await profileHubPage.waitForPageLoad();
    });

    await test.step('Navigate to orders tab', async () => {
      await profileHubPage.navigateToOrders();
      await page.waitForLoadState('domcontentloaded');
      await expect(page).toHaveURL(/\/profile\/orders/);
    });

    await test.step('Navigate to settings tab', async () => {
      await profileHubPage.navigateToSettings();
      await page.waitForLoadState('domcontentloaded');
      await expect(page).toHaveURL(/\/profile\/settings/);
    });
  });

  test('should display user profile information', async ({ profileHubPage, page }) => {
    await test.step('Navigate to profile settings', async () => {
      await profileHubPage.goto();
      await profileHubPage.navigateToSettings();
      await profileHubPage.waitForPageLoad();
    });

    await test.step('Verify Profile Information heading', async () => {
      await expect(page.getByRole('heading', { name: 'Profile Information' })).toBeVisible();
    });
  });

  test('should show change password section', async ({ profileHubPage, page }) => {
    await test.step('Navigate to profile settings', async () => {
      await profileHubPage.goto();
      await profileHubPage.navigateToSettings();
      await profileHubPage.waitForPageLoad();
    });

    await test.step('Verify Change Password heading', async () => {
      await expect(page.getByRole('heading', { name: 'Change Password' })).toBeVisible();
    });
  });

  test('should show order history on orders tab', async ({ profileHubPage }) => {
    await test.step('Navigate to profile orders', async () => {
      await profileHubPage.goto();
      await profileHubPage.navigateToOrders();
      await profileHubPage.waitForPageLoad();
    });

    await test.step('Verify order history or empty state', async () => {
      const hasOrders = await profileHubPage.getOrderCount();
      const isEmpty = await profileHubPage.emptyOrdersMessage.isVisible();
      expect(hasOrders > 0 || isEmpty).toBe(true);
    });
  });
});
