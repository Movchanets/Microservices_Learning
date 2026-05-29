import { authTest as test, expect } from '../fixtures/auth.fixture';
import { ProfileHubPage } from '../pages/profile-hub.page';

test.describe('User Profile Hub', () => {

  test('should display profile hub with sidebar navigation', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    const profileHubPage = new ProfileHubPage(page);

    await test.step('Navigate to profile hub', async () => {
      await profileHubPage.goto();
      await profileHubPage.waitForPageLoad();
    });

    await test.step('Verify page heading is visible', async () => {
      await expect(profileHubPage.pageHeading).toBeVisible();
    });

    await page.close();
  });

  test('should navigate between profile tabs', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    const profileHubPage = new ProfileHubPage(page);

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

    await page.close();
  });

  test('should display user profile information', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    const profileHubPage = new ProfileHubPage(page);

    await test.step('Navigate to profile settings', async () => {
      await profileHubPage.goto();
      await profileHubPage.navigateToSettings();
      await profileHubPage.waitForPageLoad();
    });

    await test.step('Verify Profile Information heading', async () => {
      await expect(page.getByRole('heading', { name: 'Profile Information' })).toBeVisible();
    });

    await page.close();
  });

  test('should show change password section', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    const profileHubPage = new ProfileHubPage(page);

    await test.step('Navigate to profile settings', async () => {
      await profileHubPage.goto();
      await profileHubPage.navigateToSettings();
      await profileHubPage.waitForPageLoad();
    });

    await test.step('Verify Change Password heading', async () => {
      await expect(page.getByRole('heading', { name: 'Change Password' })).toBeVisible();
    });

    await page.close();
  });

  test('should show order history on orders tab', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    const profileHubPage = new ProfileHubPage(page);

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

    await page.close();
  });
});
