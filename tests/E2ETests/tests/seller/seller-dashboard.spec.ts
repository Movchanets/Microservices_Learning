import { authTest as test, expect } from '../../fixtures/auth.fixture';

test.describe('Seller: Dashboard', () => {

  test('should redirect unauthenticated from seller dashboard', async ({ browser }) => {
    const page = await browser.newPage();
    await page.goto('/seller');
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/auth\/login/);
    await page.close();
  });

  test('should show seller dashboard for seller users', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    await page.goto('/seller');
    await page.waitForLoadState('domcontentloaded');

    await expect(page.getByRole('heading', { name: 'Seller Dashboard' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Products' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Orders' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Settings' })).toBeVisible();
    await page.close();
  });

  test('should navigate to seller products', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    await page.goto('/seller/products');
    await page.waitForLoadState('domcontentloaded');

    const heading = page.getByRole('heading').filter({ hasText: /products/i });
    await expect(heading.first()).toBeVisible();
    await page.close();
  });

  test('should navigate to store settings', async ({ sellerContext }) => {
    const page = await sellerContext.newPage();
    await page.goto('/seller/settings');
    await page.waitForLoadState('domcontentloaded');

    await expect(page.getByRole('heading', { name: 'Store Settings' })).toBeVisible();
    await page.close();
  });
});
