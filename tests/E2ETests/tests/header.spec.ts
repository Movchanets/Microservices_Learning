import { test, expect } from '../fixtures/test-base';
import { ensureAuthenticatedPageViaApi } from '../utils/api-helpers';
import { HeaderComponent } from '../components/header.component';
import { ProfilePage } from '../pages/profile.page';

test.describe('Header: Navigation & Auth State', () => {

  test('should show user dropdown after login', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request, { firstName: 'Header', lastName: 'Tester' });
    const header = new HeaderComponent(page);

    // Verify user menu trigger is visible
    await expect(header.userMenuTrigger).toBeVisible();

    // Open menu and check links
    await header.openUserMenu();
    await expect(header.profileLink).toBeVisible();
    await expect(header.logoutLink).toBeVisible();
    await context.close();
  });

  test('should navigate to profile page via header dropdown', async ({ browser, playwright }) => {
    const { page, context, email } = await ensureAuthenticatedPageViaApi(browser, playwright.request, { firstName: 'Nav', lastName: 'Tester' });
    const header = new HeaderComponent(page);
    const profilePage = new ProfilePage(page);

    // Navigate to profile
    await header.clickProfile();
    await expect(page).toHaveURL('/profile');

    // Verify profile content
    await profilePage.expectUserDetails('Nav', 'Tester', email);
    await context.close();
  });

  test('should logout successfully via header dropdown', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request, { firstName: 'Logout', lastName: 'User' });
    const header = new HeaderComponent(page);

    // Logout
    await header.logout();

    // Verify redirect to login
    await expect(page).toHaveURL(/\/auth\/login/);

    // Verify user menu trigger is NOT visible
    await expect(header.userMenuTrigger).not.toBeVisible();
    await expect(header.loginLink).toBeVisible();
    await context.close();
  });

});
