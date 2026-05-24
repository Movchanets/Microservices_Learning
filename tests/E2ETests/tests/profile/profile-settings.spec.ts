import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { ProfileHubPage } from '../../pages/profile-hub.page';

test.describe('Profile: Settings & Password Change', () => {

  test('should display profile settings with current user info', async ({ buyerContext, buyerUser }) => {
    const page = await buyerContext.newPage();
    const profilePage = new ProfileHubPage(page);

    await page.goto('/profile/settings');
    await page.waitForLoadState('domcontentloaded');

    // Should show profile information heading
    await expect(page.getByRole('heading', { name: /profile information/i })).toBeVisible();

    // Should show pre-filled name fields
    const firstNameInput = page.getByLabel(/first name/i).or(page.getByPlaceholder(/first name/i));
    await expect(firstNameInput).toBeVisible();
    await page.close();
  });

  test('should update first and last name successfully', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    const profilePage = new ProfileHubPage(page);

    await page.goto('/profile/settings');
    await page.waitForLoadState('domcontentloaded');

    await profilePage.updateProfile('UpdatedFirst', 'UpdatedLast');
    await page.waitForLoadState('domcontentloaded');

    // Should show success feedback
    const hasSuccess = await page.getByText(/success|updated|saved/i).isVisible().catch(() => false);
    expect(hasSuccess).toBe(true);
    await page.close();
  });

  test('should show change password section', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();

    await page.goto('/profile/settings');
    await page.waitForLoadState('domcontentloaded');

    await expect(page.getByRole('heading', { name: /change password/i })).toBeVisible();
    await expect(page.getByLabel(/current password/i)).toBeVisible();
    await expect(page.getByLabel(/new password/i)).toBeVisible();
    await page.close();
  });

  test('should show error for wrong current password', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    const profilePage = new ProfileHubPage(page);

    await page.goto('/profile/settings');
    await page.waitForLoadState('domcontentloaded');

    await profilePage.changePassword('WrongPassword123!', 'NewPassword123!', 'NewPassword123!');

    // Should show error — use role=alert which is accessible
    const errorAlert = page.locator('[role="alert"]').first();
    await expect(errorAlert).toBeVisible({ timeout: 5000 });
    await page.close();
  });

  test('should redirect unauthenticated from profile settings', async ({ browser }) => {
    const page = await browser.newPage();
    await page.goto('/profile/settings');
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/auth\/login/);
    await page.close();
  });
});
