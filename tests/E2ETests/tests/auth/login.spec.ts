import { test, expect } from '../../fixtures/test-base';
import { TIMEOUTS } from '../../utils/constants';
import { ensureAuthenticatedPageViaApi } from '../../utils/api-helpers';
import { LoginPage } from '../../pages/login.page';

test.describe('Authentication: Login', () => {

  test('should login successfully with newly registered user', async ({ browser, playwright }) => {
    const { page, context, email, password } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const loginPage = new LoginPage(page);

    await test.step('Verify initial authenticated redirect to catalog', async () => {
      await expect(page).toHaveURL('/catalog');
    });

    await test.step('Clear session to test re-login', async () => {
      await page.context().clearCookies();
      await page.evaluate(() => localStorage.clear());
    });

    await test.step('Login with the same credentials', async () => {
      await loginPage.goto('/auth/login');
      await loginPage.login(email, password);
    });

    await test.step('Verify redirect to catalog after login', async () => {
      await expect(page).toHaveURL('/catalog');
    });

    await context.close();
  });

  test('should show error with invalid credentials', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await test.step('Navigate to login page', async () => {
      await loginPage.goto('/auth/login');
      // Wait for Angular hydration — form must be interactive
      await expect(loginPage.emailInput).toBeVisible({ timeout: TIMEOUTS.element });
    });

    await test.step('Attempt login with invalid credentials', async () => {
      await loginPage.login('nonexistent@test.com', 'WrongPassword123!');
    });

    await test.step('Verify error message is shown', async () => {
      await loginPage.waitForErrorMessage('401', 15_000);
    });
  });

});