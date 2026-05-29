import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { SellerDashboardPage } from '../../pages/seller-dashboard.page';

test.describe('Seller: Dashboard', () => {

  test('should redirect unauthenticated from seller dashboard', async ({ browser }) => {
    const page = await browser.newPage();

    await test.step('Navigate to seller page without authentication', async () => {
      await page.goto('/seller');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify redirect to login page', async () => {
      await expect(page).toHaveURL(/\/auth\/login/);
    });

    await page.close();
  });

  test('should show seller dashboard for seller users', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    const dashboardPage = new SellerDashboardPage(page);

    await test.step('Navigate to seller dashboard', async () => {
      await page.goto('/seller');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify dashboard heading and navigation tabs', async () => {
      await expect(dashboardPage.pageHeading).toBeVisible();
      await expect(dashboardPage.productsTab).toBeVisible();
      await expect(page.getByRole('link', { name: 'Orders' })).toBeVisible();
      await expect(dashboardPage.settingsTab).toBeVisible();
    });

    await page.close();
  });

  test('should navigate to seller products', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();

    await test.step('Navigate to seller products page', async () => {
      await page.goto('/seller/products');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify products heading is visible', async () => {
      const heading = page.getByRole('heading').filter({ hasText: /products/i });
      await expect(heading.first()).toBeVisible();
    });

    await page.close();
  });

  test('should navigate to store settings', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();

    await test.step('Navigate to store settings page', async () => {
      await page.goto('/seller/settings');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify store settings heading', async () => {
      await expect(page.getByRole('heading', { name: 'Store Settings' })).toBeVisible();
    });

    await page.close();
  });
});
