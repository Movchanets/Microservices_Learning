import { test, expect } from '../../fixtures/test-base';
import * as users from '../../data/users.json';

test.describe('Auth: Profile', () => {

  test('should display user profile after login', async ({ page }) => {
    // Login
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.validUser.email);
    await page.getByPlaceholder('••••••••').fill(users.validUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/catalog/);

    // Navigate to profile via header dropdown
    await page.getByTestId('user-menu-trigger').click();
    await page.getByRole('link', { name: /profile/i }).click();
    await page.waitForLoadState('networkidle');

    // Should see profile page with user info
    await expect(page.getByText(users.validUser.email)).toBeVisible();
  });

  test('should show user email', async ({ page }) => {
    // Login
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.validUser.email);
    await page.getByPlaceholder('••••••••').fill(users.validUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/catalog/);

    // Navigate to profile
    await page.getByTestId('user-menu-trigger').click();
    await page.getByRole('link', { name: /profile/i }).click();
    await page.waitForLoadState('networkidle');

    // Should display user email
    await expect(page.getByText(users.validUser.email)).toBeVisible();
  });

  test('should have logout button', async ({ page }) => {
    // Login
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.validUser.email);
    await page.getByPlaceholder('••••••••').fill(users.validUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/catalog/);

    // Navigate to profile
    await page.getByTestId('user-menu-trigger').click();
    await page.getByRole('link', { name: /profile/i }).click();
    await page.waitForLoadState('networkidle');

    // Should have sign out button
    await expect(page.getByRole('button', { name: /sign out/i })).toBeVisible();
  });

  test('should redirect to login when not authenticated', async ({ page }) => {
    // Try to access profile without login
    await page.goto('/profile');
    await page.waitForLoadState('networkidle');

    // Should be redirected to login
    await expect(page).toHaveURL(/\/auth\/login/);
  });
});
