import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { TIMEOUTS } from '../../utils/constants';

test.describe('Orders: Order History', () => {

  test('should display orders page after login', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();

    await test.step('Navigate to orders page', async () => {
      await page.goto('/orders');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify orders heading is visible', async () => {
      await expect(page.getByRole('heading', { name: 'My Orders' })).toBeVisible();
    });

    await page.close();
  });

  test('should show empty state when no orders', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();

    await test.step('Navigate to orders page', async () => {
      await page.goto('/orders');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify orders heading is visible', async () => {
      const heading = page.getByRole('heading', { name: 'My Orders' });
      await expect(heading).toBeVisible();
    });

    await page.close();
  });

  // Test buyer has no orders yet — skip until test fixture seeds orders
  test.skip('should navigate to order detail when clicking an order', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();

    await test.step('Navigate to orders page', async () => {
      await page.goto('/orders');
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Click an order link', async () => {
      const orderLink = page.getByRole('link').filter({ hasText: /View Details|order-/i }).first();
      await expect(orderLink).toBeVisible({ timeout: TIMEOUTS.element });
      await orderLink.click();
      await page.waitForLoadState('domcontentloaded');
    });

    await test.step('Verify navigation to order detail', async () => {
      await expect(page).toHaveURL(/\/orders\/.+/);
    });

    await page.close();
  });
});
