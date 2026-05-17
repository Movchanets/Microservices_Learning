import { test, expect } from '../fixtures/test-base';

test.describe('Plan 02: User Profile Hub', () => {

  let testEmail: string;
  let testPassword: string;

  test.beforeEach(async ({ loginPage, registerPage, page }) => {
    const randomId = Math.random().toString(36).substring(7);
    testEmail = `user_${randomId}@test.com`;
    testPassword = 'P@ssw0rd123!';

    await registerPage.goto('/auth/register');
    await registerPage.register('Test', 'User', testEmail, testPassword);
    await page.waitForLoadState('networkidle');

    if (page.url().includes('/auth/login')) {
      await loginPage.login(testEmail, testPassword);
      await expect(page).toHaveURL(/\/catalog/);
    }
  });

  test('should display profile hub with sidebar navigation', async ({ page, profileHubPage }) => {
    await profileHubPage.goto();
    await profileHubPage.waitForPageLoad();
    await expect(profileHubPage.pageHeading).toBeVisible();
  });

  test('should navigate between profile tabs', async ({ page, profileHubPage }) => {
    await profileHubPage.goto();
    await profileHubPage.waitForPageLoad();

    await profileHubPage.navigateToOrders();
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/profile\/orders/);

    await profileHubPage.navigateToSettings();
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/profile\/settings/);
  });

  test('should display user profile information', async ({ page, profileHubPage }) => {
    await profileHubPage.goto();
    await profileHubPage.navigateToSettings();
    await profileHubPage.waitForPageLoad();
    await expect(page.getByRole('heading', { name: 'Profile Information' })).toBeVisible();
  });

  test('should show change password section', async ({ page, profileHubPage }) => {
    await profileHubPage.goto();
    await profileHubPage.navigateToSettings();
    await profileHubPage.waitForPageLoad();
    await expect(page.getByRole('heading', { name: 'Change Password' })).toBeVisible();
  });

  test('should show order history on orders tab', async ({ page, profileHubPage }) => {
    await profileHubPage.goto();
    await profileHubPage.navigateToOrders();
    await profileHubPage.waitForPageLoad();

    const hasOrders = await profileHubPage.getOrderCount();
    const isEmpty = await profileHubPage.emptyOrdersMessage.isVisible();
    expect(hasOrders > 0 || isEmpty).toBe(true);
  });
});
