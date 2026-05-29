import { authTest as test, expect } from '../../fixtures/auth.fixture';

test.describe('Auth: Profile', () => {

  test('should display user profile after login', async ({ adminContext, adminUser }) => {
    const page = await adminContext.newPage();

    await test.step('Navigate to profile page', async () => {
      await page.goto('/profile');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify user email is displayed', async () => {
      await expect(page.getByText(adminUser.email)).toBeVisible();
    });

    await page.close();
  });

  test('should have logout button', async ({ adminContext }) => {
    const page = await adminContext.newPage();

    await test.step('Navigate to profile page', async () => {
      await page.goto('/profile');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify logout button is visible', async () => {
      await expect(page.getByRole('button', { name: /sign out/i })).toBeVisible();
    });

    await page.close();
  });

  test('should redirect to login when not authenticated', async ({ browser }) => {
    const page = await browser.newPage();

    await test.step('Navigate to profile without authentication', async () => {
      await page.goto('/profile');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify redirect to login page', async () => {
      await expect(page).toHaveURL(/\/auth\/login/);
    });

    await page.close();
  });
});
