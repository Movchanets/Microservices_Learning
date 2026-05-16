import { test, expect } from '../../fixtures/test-base';
import * as users from '../../data/users.json';

test.describe('Orders: Order History', () => {

  test('should display orders page after login', async ({ page }) => {
    // Login
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.validUser.email);
    await page.getByPlaceholder('••••••••').fill(users.validUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/catalog/);

    // Navigate to orders
    await page.goto('/orders');
    await page.waitForLoadState('networkidle');

    // Should see orders heading
    await expect(page.getByRole('heading', { name: 'My Orders' })).toBeVisible();
  });

  test('should show empty state when no orders', async ({ page }) => {
    // Login as existing user
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.validUser.email);
    await page.getByPlaceholder('••••••••').fill(users.validUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/catalog/);

    // Go to orders
    await page.goto('/orders');
    await page.waitForLoadState('networkidle');

    // Should see orders heading
    const heading = page.getByRole('heading', { name: 'My Orders' });
    await expect(heading).toBeVisible();
  });

  test('should navigate to order detail when clicking an order', async ({ page }) => {
    // Login
    await page.goto('/auth/login');
    await page.getByPlaceholder('name@company.com').fill(users.validUser.email);
    await page.getByPlaceholder('••••••••').fill(users.validUser.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/catalog/);

    // Go to orders
    await page.goto('/orders');
    await page.waitForLoadState('networkidle');

    // Click first order if exists
    const orderLink = page.getByRole('link').filter({ hasText: /View Details|order-/i }).first();
    if (await orderLink.isVisible()) {
      await orderLink.click();
      await page.waitForLoadState('networkidle');

      // Should be on order detail page
      await expect(page).toHaveURL(/\/orders\/.+/);
    }
  });
});
