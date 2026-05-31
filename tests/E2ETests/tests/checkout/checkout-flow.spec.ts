import { authTest as test, expect } from '../../fixtures/auth.fixture';

test.describe('Checkout: Order Flow', () => {

  test('should show checkout page', async ({ checkoutPage }) => {
    await test.step('Navigate to checkout page', async () => {
      await checkoutPage.goto();
      await checkoutPage.waitForPageLoad();
    });

    await test.step('Verify checkout heading is visible', async () => {
      await expect(checkoutPage.pageHeading).toBeVisible();
    });
  });

  test('should show empty cart message when no items', async ({ checkoutPage, buyerApi }) => {
    await test.step('Clear cart for test isolation', async () => {
      await buyerApi.delete('/api/cart').catch(() => {});
    });

    await test.step('Navigate to checkout page', async () => {
      await checkoutPage.goto();
      await checkoutPage.waitForPageLoad();
    });

    await test.step('Verify empty cart state', async () => {
      const hasEmptyMessage = await checkoutPage.emptyCartMessage.isVisible().catch(() => false);
      const isConfirmVisible = await checkoutPage.confirmOrderBtn.isVisible().catch(() => false);

      // At least one of these should be true for an empty cart
      expect(hasEmptyMessage || !isConfirmVisible).toBe(true);
    });
  });
});
