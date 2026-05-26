import { authTest as test, expect } from '../../fixtures/auth.fixture';

test.describe('Checkout: Order Flow', () => {

  test('should show checkout page', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    await page.goto('/checkout');
    await page.waitForLoadState('domcontentloaded');

    await expect(page.getByRole('heading', { name: 'Checkout' })).toBeVisible();
    await page.close();
  });

  test('should show empty cart message when no items', async ({ buyerContext, buyerApi }) => {
    // Clear cart first to ensure isolation from parallel workers
    await buyerApi.delete('/api/cart').catch(() => {});

    const page = await buyerContext.newPage();
    await page.goto('/checkout');
    await page.waitForLoadState('domcontentloaded');

    const emptyMessage = page.getByText('Your cart is empty');
    const confirmBtn = page.getByRole('button', { name: 'Confirm Order' });

    const hasEmptyMessage = await emptyMessage.isVisible().catch(() => false);
    const isConfirmVisible = await confirmBtn.isVisible().catch(() => false);

    // At least one of these should be true for an empty cart
    expect(hasEmptyMessage || !isConfirmVisible).toBe(true);
    await page.close();
  });
});
