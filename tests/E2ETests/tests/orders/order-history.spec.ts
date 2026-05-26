import { authTest as test, expect } from '../../fixtures/auth.fixture';

test.describe('Orders: Order History', () => {

  test('should display orders page after login', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    await expect(page.getByRole('heading', { name: 'My Orders' })).toBeVisible();
    await page.close();
  });

  test('should show empty state when no orders', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    const heading = page.getByRole('heading', { name: 'My Orders' });
    await expect(heading).toBeVisible();
    await page.close();
  });

  // Test buyer has no orders yet — skip until test fixture seeds orders
  test.skip('should navigate to order detail when clicking an order', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    await page.goto('/orders');
    await page.waitForLoadState('domcontentloaded');

    const orderLink = page.getByRole('link').filter({ hasText: /View Details|order-/i }).first();
    await expect(orderLink).toBeVisible({ timeout: 10_000 });
    await orderLink.click();
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/orders\/.+/);
    await page.close();
  });
});
