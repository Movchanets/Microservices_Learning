import { authTest as test, expect } from '../../fixtures/auth.fixture';

test.describe('Admin: Panel', () => {

  test('should redirect unauthenticated from admin panel', async ({ browser }) => {
    const page = await browser.newPage();
    await page.goto('/admin');
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/auth\/login/);
    await page.close();
  });

  test('should show admin panel for admin users', async ({ adminContext }) => {
    const page = await adminContext.newPage();
    await page.goto('/admin');
    await page.waitForLoadState('domcontentloaded');

    await expect(page.getByRole('heading', { name: 'Admin Panel' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Users' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Verifications' })).toBeVisible();
    await page.close();
  });

  test('should display users list', async ({ adminContext }) => {
    const page = await adminContext.newPage();
    await page.goto('/admin/users');
    await page.waitForLoadState('domcontentloaded');

    const table = page.getByRole('table');
    await expect(table).toBeVisible();

    const rows = table.locator('tbody tr');
    const rowCount = await rows.count();
    expect(rowCount).toBeGreaterThan(0);
    await page.close();
  });

  test('should navigate to verifications tab', async ({ adminContext }) => {
    const page = await adminContext.newPage();
    await page.goto('/admin/verifications');
    await page.waitForLoadState('domcontentloaded');

    const heading = page.getByRole('heading', { name: 'Admin Panel' });
    await expect(heading).toBeVisible();
    await page.close();
  });

  test('should show admin link in header for admin users', async ({ adminContext }) => {
    const page = await adminContext.newPage();
    await page.goto('/admin');
    await page.waitForLoadState('domcontentloaded');

    // Open the user dropdown menu first — admin link is inside it
    const menuTrigger = page.getByTestId('user-menu-trigger');
    await expect(menuTrigger).toBeVisible({ timeout: 10000 });
    await menuTrigger.click();

    const adminLink = page.getByTestId('nav-admin');
    await expect(adminLink).toBeVisible({ timeout: 5000 });
    await page.close();
  });

  test('should NOT show admin link for non-admin users', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    await page.goto('/catalog');
    await page.waitForLoadState('domcontentloaded');

    const adminLink = page.getByTestId('nav-admin');
    await expect(adminLink).not.toBeVisible();
    await page.close();
  });
});
