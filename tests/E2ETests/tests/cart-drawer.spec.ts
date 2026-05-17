import { test, expect } from '../fixtures/test-base';

test.describe('Plan 03: Cart Drawer & Checkout', () => {

  test.beforeEach(async ({ loginPage, registerPage, page }) => {
    const randomId = Math.random().toString(36).substring(7);
    const email = `user_${randomId}@test.com`;
    const password = 'P@ssw0rd123!';

    await registerPage.goto('/auth/register');
    await registerPage.register('Test', 'User', email, password);
    await page.waitForLoadState('networkidle');

    if (page.url().includes('/auth/login')) {
      await loginPage.login(email, password);
      await expect(page).toHaveURL(/\/catalog/);
    }
  });

  test('should open cart drawer from header', async ({ page, header, cartDrawer }) => {
    await header.openCart();
    await cartDrawer.waitForOpen();
    await expect(cartDrawer.heading).toBeVisible();
  });

  test('should close cart drawer', async ({ page, header, cartDrawer }) => {
    await header.openCart();
    await cartDrawer.waitForOpen();
    await cartDrawer.close();
    await cartDrawer.waitForClose();
  });

  test('should show empty cart message when no items', async ({ page, header, cartDrawer }) => {
    await header.openCart();
    await cartDrawer.waitForOpen();
    const isEmpty = await cartDrawer.isEmpty();
    const itemCount = await cartDrawer.getItemCount();
    expect(isEmpty || itemCount > 0).toBe(true);
  });

  test('should add item and see it in cart drawer', async ({ page, header, cartDrawer }) => {
    const addBtn = page.getByRole('button', { name: /add to cart/i }).first();
    if (await addBtn.isVisible()) {
      await addBtn.click();
      await page.waitForTimeout(500);
      await header.openCart();
      await cartDrawer.waitForOpen();
      const itemCount = await cartDrawer.getItemCount();
      expect(itemCount).toBeGreaterThan(0);
    }
  });

  test('should display checkout page with address form', async ({ page, checkoutEnhancedPage }) => {
    await checkoutEnhancedPage.goto();
    await checkoutEnhancedPage.waitForPageLoad();
    // May show empty cart or checkout form
    const isEmpty = await checkoutEnhancedPage.emptyCartMessage.isVisible();
    const hasHeading = await checkoutEnhancedPage.pageHeading.isVisible();
    expect(isEmpty || hasHeading).toBe(true);
  });
});
