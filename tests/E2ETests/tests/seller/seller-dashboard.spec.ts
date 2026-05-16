import { test, expect } from '../../fixtures/test-base';
import * as users from '../../data/users.json';

test.describe('Seller: Dashboard', () => {

  test('should redirect unauthenticated from seller dashboard', async ({ page }) => {
    // Try to access seller dashboard without login
    await page.goto('/seller');
    await page.waitForLoadState('networkidle');

    // Should be redirected to login
    await expect(page).toHaveURL(/\/auth\/login/);
  });

  test('should show seller dashboard for seller users', async ({ page }) => {
    // Login as seller
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.sellerUser.email);
    await page.getByPlaceholder('••••••••').fill(users.sellerUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForLoadState('networkidle');

    // Navigate to seller dashboard
    await page.goto('/seller');
    await page.waitForLoadState('networkidle');

    // Should see seller dashboard heading
    await expect(page.getByRole('heading', { name: 'Seller Dashboard' })).toBeVisible();

    // Should have navigation tabs
    await expect(page.getByRole('link', { name: 'Products' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Orders' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Settings' })).toBeVisible();
  });

  test('should navigate to seller products', async ({ page }) => {
    // Login as seller
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.sellerUser.email);
    await page.getByPlaceholder('••••••••').fill(users.sellerUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForLoadState('networkidle');

    // Go to seller products
    await page.goto('/seller/products');
    await page.waitForLoadState('networkidle');

    // Should see products section
    const heading = page.getByRole('heading').filter({ hasText: /products/i });
    await expect(heading.first()).toBeVisible();
  });

  test('should navigate to store settings', async ({ page }) => {
    // Login as seller
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.sellerUser.email);
    await page.getByPlaceholder('••••••••').fill(users.sellerUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForLoadState('networkidle');

    // Go to store settings
    await page.goto('/seller/settings');
    await page.waitForLoadState('networkidle');

    // Should see store settings
    await expect(page.getByRole('heading', { name: 'Store Settings' })).toBeVisible();
  });
});
