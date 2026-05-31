import { authTest as test, expect } from '../../fixtures/auth.fixture';

test.describe('Auth: Profile', () => {

  test('should display user profile after login', async ({ profilePage, adminUser }) => {
    await test.step('Navigate to profile page', async () => {
      await profilePage.goto();
      await profilePage.waitForPageLoad();
    });

    await test.step('Verify user email is displayed', async () => {
      await expect(profilePage.page.getByText(adminUser.email)).toBeVisible();
    });
  });

  test('should have logout button', async ({ profilePage }) => {
    await test.step('Navigate to profile page', async () => {
      await profilePage.goto();
      await profilePage.waitForPageLoad();
    });

    await test.step('Verify logout button is visible', async () => {
      await expect(profilePage.logoutBtn).toBeVisible();
    });
  });
});
