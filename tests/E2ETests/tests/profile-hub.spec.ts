import { test, expect } from '../fixtures/test-base';
import { ensureAuthenticatedPageViaApi } from '../utils/api-helpers';
import { ProfileHubPage } from '../pages/profile-hub.page';

test.describe('User Profile Hub', () => {

  test('should display profile hub with sidebar navigation', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const profileHubPage = new ProfileHubPage(page);

    await profileHubPage.goto();
    await profileHubPage.waitForPageLoad();
    await expect(profileHubPage.pageHeading).toBeVisible();
    await context.close();
  });

  test('should navigate between profile tabs', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const profileHubPage = new ProfileHubPage(page);

    await profileHubPage.goto();
    await profileHubPage.waitForPageLoad();

    await profileHubPage.navigateToOrders();
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/profile\/orders/);

    await profileHubPage.navigateToSettings();
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/profile\/settings/);
    await context.close();
  });

  test('should display user profile information', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const profileHubPage = new ProfileHubPage(page);

    await profileHubPage.goto();
    await profileHubPage.navigateToSettings();
    await profileHubPage.waitForPageLoad();
    await expect(page.getByRole('heading', { name: 'Profile Information' })).toBeVisible();
    await context.close();
  });

  test('should show change password section', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const profileHubPage = new ProfileHubPage(page);

    await profileHubPage.goto();
    await profileHubPage.navigateToSettings();
    await profileHubPage.waitForPageLoad();
    await expect(page.getByRole('heading', { name: 'Change Password' })).toBeVisible();
    await context.close();
  });

  test('should show order history on orders tab', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const profileHubPage = new ProfileHubPage(page);

    await profileHubPage.goto();
    await profileHubPage.navigateToOrders();
    await profileHubPage.waitForPageLoad();

    const hasOrders = await profileHubPage.getOrderCount();
    const isEmpty = await profileHubPage.emptyOrdersMessage.isVisible();
    expect(hasOrders > 0 || isEmpty).toBe(true);
    await context.close();
  });
});
