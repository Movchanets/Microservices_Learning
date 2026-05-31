import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { TIMEOUTS } from '../../utils/constants';

test.describe('Admin: Panel', () => {

  test('should show admin panel for admin users', async ({ adminPage }) => {
    await test.step('Navigate to /admin as admin', async () => {
      await adminPage.goto();
      await adminPage.waitForPageLoad();
    });

    await test.step('Verify admin panel elements', async () => {
      await expect(adminPage.pageHeading).toBeVisible();
      await expect(adminPage.usersTab).toBeVisible();
      await expect(adminPage.verificationsTab).toBeVisible();
    });
  });

  test('should display users list', async ({ adminPage }) => {
    await test.step('Navigate to users list', async () => {
      await adminPage.goto();
      await adminPage.navigateToUsers();
    });

    await test.step('Verify table has rows', async () => {
      await expect(adminPage.usersTable).toBeVisible();
      const rowCount = await adminPage.getUserCount();
      expect(rowCount).toBeGreaterThan(0);
    });
  });

  test('should navigate to verifications tab', async ({ adminPage }) => {
    await test.step('Navigate to verifications', async () => {
      await adminPage.goto();
      await adminPage.navigateToVerifications();
    });

    await test.step('Verify admin panel heading', async () => {
      await expect(adminPage.pageHeading).toBeVisible();
    });
  });

  test('should show admin link in header for admin users', async ({ adminPage, header }) => {
    await test.step('Navigate to /admin as admin', async () => {
      await adminPage.goto();
      await adminPage.waitForPageLoad();
    });

    await test.step('Open user dropdown menu', async () => {
      await expect(header.userMenuTrigger).toBeVisible({ timeout: TIMEOUTS.element });
      await header.openUserMenu();
    });

    await test.step('Verify admin link is visible', async () => {
      await expect(header.adminLink).toBeVisible({ timeout: TIMEOUTS.quick });
    });
  });

  test('should NOT show admin link for non-admin users', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();

    await test.step('Navigate to /catalog as seller', async () => {
      await page.goto('/catalog');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify admin link is hidden', async () => {
      const adminLink = page.getByTestId('nav-admin');
      await expect(adminLink).not.toBeVisible();
    });

    await page.close();
  });
});
