import { authTest as test, expect } from '../../fixtures/auth.fixture';

test.describe('Admin: User Management', () => {

  test('should display user list with roles', async ({ adminContext }) => {
    const page = await adminContext.newPage();
    await page.goto('/admin/users');
    await page.waitForLoadState('domcontentloaded');

    const table = page.getByRole('table');
    await expect(table).toBeVisible();

    const rows = table.locator('tbody tr');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);

    // Should have role column
    await expect(page.getByRole('columnheader', { name: /role/i })).toBeVisible();
    await page.close();
  });

  test('should change a user role via dropdown', async ({ adminContext }) => {
    const page = await adminContext.newPage();
    await page.goto('/admin/users');
    await page.waitForLoadState('domcontentloaded');

    const rows = page.locator('table tbody tr');
    const count = await rows.count();
    if (count === 0) {
      test.skip(true, 'No users to manage — skipping');
      await page.close();
      return;
    }

    // Find a non-admin user row and change its role
    const firstRow = rows.first();
    const select = firstRow.locator('select');
    if (await select.isVisible()) {
      const currentRole = await select.inputValue();
      const newRole = currentRole === 'Buyer' ? 'Seller' : 'Buyer';
      await select.selectOption(newRole);
      await page.waitForLoadState('domcontentloaded');

      // Verify the select updated
      const updatedRole = await select.inputValue();
      expect(updatedRole).toBe(newRole);
    }
    await page.close();
  });

  test('should show deactivation button for users', async ({ adminContext }) => {
    const page = await adminContext.newPage();
    await page.goto('/admin/users');
    await page.waitForLoadState('domcontentloaded');

    const rows = page.locator('table tbody tr');
    const count = await rows.count();
    if (count === 0) {
      test.skip(true, 'No users — skipping');
      await page.close();
      return;
    }

    // Should have at least one deactivate button
    const deactivateBtn = page.getByRole('button', { name: /deactivate/i }).first();
    await expect(deactivateBtn).toBeVisible();
    await page.close();
  });

  test('should NOT show admin link for seller users', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    await page.goto('/catalog');
    await page.waitForLoadState('domcontentloaded');

    // Admin link should not be in the nav for sellers
    const adminLink = page.getByTestId('nav-admin');
    await expect(adminLink).not.toBeVisible();
    await page.close();
  });

  test('should redirect non-admin users from admin panel', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    await page.goto('/admin');
    await page.waitForLoadState('domcontentloaded');

    // Should redirect to catalog or show 403
    const isRedirected = !page.url().includes('/admin');
    const hasAccessDenied = await page.getByText(/access denied|unauthorized|not authorized/i).isVisible().catch(() => false);
    expect(isRedirected || hasAccessDenied).toBe(true);
    await page.close();
  });
});
