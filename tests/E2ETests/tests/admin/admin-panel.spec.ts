import { test, expect } from '../../fixtures/test-base';
import * as users from '../../data/users.json';

test.describe('Admin: Panel', () => {

  test('should redirect unauthenticated from admin panel', async ({ page }) => {
    // Try to access admin panel without login
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');

    // Should be redirected to login
    await expect(page).toHaveURL(/\/auth\/login/);
  });

  test('should show admin panel for admin users', async ({ page }) => {
    // Login as admin
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.adminUser.email);
    await page.getByPlaceholder('••••••••').fill(users.adminUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForLoadState('networkidle');

    // Navigate to admin panel
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');

    // Should see admin panel heading
    await expect(page.getByRole('heading', { name: 'Admin Panel' })).toBeVisible();

    // Should have navigation tabs
    await expect(page.getByRole('link', { name: 'Users' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Verifications' })).toBeVisible();
  });

  test('should display users list', async ({ page }) => {
    // Login as admin
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.adminUser.email);
    await page.getByPlaceholder('••••••••').fill(users.adminUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForLoadState('networkidle');

    // Go to admin users
    await page.goto('/admin/users');
    await page.waitForLoadState('networkidle');

    // Should see users table
    const table = page.getByRole('table');
    await expect(table).toBeVisible();

    // Should have at least one row
    const rows = table.locator('tbody tr');
    const rowCount = await rows.count();
    expect(rowCount).toBeGreaterThan(0);
  });

  test('should navigate to verifications tab', async ({ page }) => {
    // Login as admin
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.adminUser.email);
    await page.getByPlaceholder('••••••••').fill(users.adminUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForLoadState('networkidle');

    // Go to verifications
    await page.goto('/admin/verifications');
    await page.waitForLoadState('networkidle');

    // Should see verifications heading or empty state
    const heading = page.getByRole('heading', { name: 'Admin Panel' });
    await expect(heading).toBeVisible();
  });

  test('should show admin link in header for admin users', async ({ page }) => {
    // Login as admin
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.adminUser.email);
    await page.getByPlaceholder('••••••••').fill(users.adminUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForLoadState('networkidle');

    // Should see Admin link in header
    const adminLink = page.getByTestId('nav-admin');
    await expect(adminLink).toBeVisible();
  });

  test('should NOT show admin link for non-admin users', async ({ page }) => {
    // Login as seller (not admin)
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.sellerUser.email);
    await page.getByPlaceholder('••••••••').fill(users.sellerUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForLoadState('networkidle');

    // Should NOT see Admin link in header
    const adminLink = page.getByTestId('nav-admin');
    await expect(adminLink).not.toBeVisible();
  });
});
