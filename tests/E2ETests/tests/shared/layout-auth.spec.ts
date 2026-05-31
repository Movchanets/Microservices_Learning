import { test, expect } from '../../fixtures/test-base';
import { authTest } from '../../fixtures/auth.fixture';
import { TIMEOUTS } from '../../utils/constants';

authTest.describe('Shared Layout: Header (Authenticated)', () => {

  authTest('should show user menu when authenticated', async ({ homePage, header }) => {
    await test.step('Navigate to home page as authenticated user', async () => {
      await homePage.goto();
      await homePage.waitForPageLoad();
    });

    await test.step('Verify user menu trigger is visible', async () => {
      await expect(header.userMenuTrigger).toBeVisible({ timeout: TIMEOUTS.api });
    });
  });

  authTest('should open user dropdown and show profile link', async ({ homePage, header }) => {
    await test.step('Navigate to home page as authenticated user', async () => {
      await homePage.goto();
      await homePage.waitForPageLoad();
    });

    await test.step('Open user menu', async () => {
      await header.openUserMenu();
    });

    await test.step('Verify profile and logout links', async () => {
      await expect(header.profileLink).toBeVisible();
      await expect(header.logoutLink).toBeVisible();
    });
  });
});
