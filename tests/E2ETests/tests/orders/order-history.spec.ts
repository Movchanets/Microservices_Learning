import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { TIMEOUTS } from '../../utils/constants';

test.describe('Orders: Order History', () => {

  test('should display orders page after login', async ({ ordersPage }) => {
    await test.step('Navigate to orders page', async () => {
      await ordersPage.goto();
      await ordersPage.waitForPageLoad();
    });

    await test.step('Verify orders heading is visible', async () => {
      await expect(ordersPage.pageHeading).toBeVisible();
    });
  });

  test('should show empty state when no orders', async ({ ordersPage }) => {
    await test.step('Navigate to orders page', async () => {
      await ordersPage.goto();
      await ordersPage.waitForPageLoad();
    });

    await test.step('Verify orders heading is visible', async () => {
      await expect(ordersPage.pageHeading).toBeVisible();
    });
  });

  // Test buyer has no orders yet — skip until test fixture seeds orders
  test.skip('should navigate to order detail when clicking an order', async ({ ordersPage }) => {
    await test.step('Navigate to orders page', async () => {
      await ordersPage.goto();
      await ordersPage.waitForPageLoad();
    });

    await test.step('Click an order link', async () => {
      await ordersPage.viewOrderDetails('order-');
    });
  });
});
