import { test, expect } from '../../fixtures/test-base';
import { ensureAuthenticatedPageViaApi } from '../../utils/api-helpers';
import { LoginPage } from '../../pages/login.page';

test.describe('Authentication: Login', () => {

  test('should login successfully with newly registered user', async ({ browser, playwright }) => {
    const { page, context, email, password } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const loginPage = new LoginPage(page);

    // Already logged in from ensureAuthenticatedPageViaApi — verify we're on catalog
    await expect(page).toHaveURL('/catalog');

    // Clear session to test re-login
    await page.context().clearCookies();
    await page.evaluate(() => localStorage.clear());

    // Login with the same credentials
    await loginPage.goto('/auth/login');
    await loginPage.login(email, password);

    // Should redirect to catalog
    await expect(page).toHaveURL('/catalog');
    await context.close();
  });

  test('should show error with invalid credentials', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto('/auth/login');
    await loginPage.login('nonexistent@test.com', 'WrongPassword123!');

    // Verify error message is shown (actual message contains "401 Unauthorized")
    await loginPage.expectErrorMessage('401');
  });

});
