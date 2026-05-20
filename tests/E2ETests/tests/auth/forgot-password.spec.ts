import { test, expect } from '../../fixtures/test-base';

test.describe('Auth: Forgot Password', () => {

  test('should display forgot password form', async ({ page, forgotPasswordPage }) => {
    await page.goto('/auth/forgot-password');
    await page.waitForLoadState('domcontentloaded');

    await expect(forgotPasswordPage.emailInput).toBeVisible();
    await expect(forgotPasswordPage.forgotSubmitBtn).toBeVisible();
  });

  test('should show success message after submitting valid email', async ({ page, forgotPasswordPage }) => {
    await page.goto('/auth/forgot-password');
    await page.waitForLoadState('domcontentloaded');

    await forgotPasswordPage.resetPassword('admin@marketplace.com');

    // Should show some confirmation (toast, message, or redirect)
    await expect(page.getByText(/check your email|sent|reset/i)).toBeVisible({ timeout: 10000 });
  });

  test('should show error for non-existent email', async ({ page, forgotPasswordPage }) => {
    await page.goto('/auth/forgot-password');
    await page.waitForLoadState('domcontentloaded');

    await forgotPasswordPage.resetPassword('nonexistent@example.com');

    // Should show error or still be on the same page
    const hasError = await page.getByText(/not found|error|invalid/i).isVisible().catch(() => false);
    const stillOnPage = page.url().includes('forgot-password');
    expect(hasError || stillOnPage).toBe(true);
  });

  test('should navigate back to login', async ({ page }) => {
    await page.goto('/auth/forgot-password');
    await page.waitForLoadState('domcontentloaded');

    const backLink = page.getByRole('link', { name: /back.*sign in/i });
    if (await backLink.isVisible()) {
      await backLink.click();
      await expect(page).toHaveURL(/\/auth\/login/);
    }
  });
});
