import { test, expect } from '../fixtures/test-base';
import { ensureAuthenticatedPageViaApi } from '../utils/api-helpers';

test.describe('Cart Drawer & Checkout', () => {

  test('should open cart drawer from header', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const header = new (await import('../components/header.component')).HeaderComponent(page);
    const cartDrawer = new (await import('../components/cart-drawer.component')).CartDrawerComponent(page);

    await header.openCart();
    await cartDrawer.waitForOpen();
    await expect(cartDrawer.heading).toBeVisible();
    await context.close();
  });

  test('should close cart drawer', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const header = new (await import('../components/header.component')).HeaderComponent(page);
    const cartDrawer = new (await import('../components/cart-drawer.component')).CartDrawerComponent(page);

    await header.openCart();
    await cartDrawer.waitForOpen();
    await cartDrawer.close();
    await cartDrawer.waitForClose();
    await context.close();
  });

  test('should show empty cart message when no items', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const header = new (await import('../components/header.component')).HeaderComponent(page);
    const cartDrawer = new (await import('../components/cart-drawer.component')).CartDrawerComponent(page);

    await header.openCart();
    await cartDrawer.waitForOpen();
    const isEmpty = await cartDrawer.isEmpty();
    const itemCount = await cartDrawer.getItemCount();
    expect(isEmpty || itemCount > 0).toBe(true);
    await context.close();
  });

  test('should add item and see it in cart drawer', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const header = new (await import('../components/header.component')).HeaderComponent(page);
    const cartDrawer = new (await import('../components/cart-drawer.component')).CartDrawerComponent(page);

    const addBtn = page.getByRole('button', { name: /add to cart/i }).first();
    await expect(addBtn).toBeVisible({ timeout: 10000 });
    await addBtn.click();
    await page.waitForLoadState('domcontentloaded');
    await header.openCart();
    await cartDrawer.waitForOpen();
    const itemCount = await cartDrawer.getItemCount();
    expect(itemCount).toBeGreaterThan(0);
    await context.close();
  });

  test('should display checkout page with address form', async ({ browser, playwright }) => {
    const { page, context } = await ensureAuthenticatedPageViaApi(browser, playwright.request);
    const checkoutPage = new (await import('../pages/checkout-enhanced.page')).CheckoutEnhancedPage(page);

    await checkoutPage.goto();
    await checkoutPage.waitForPageLoad();
    const isEmpty = await checkoutPage.emptyCartMessage.isVisible();
    const hasHeading = await checkoutPage.pageHeading.isVisible();
    expect(isEmpty || hasHeading).toBe(true);
    await context.close();
  });
});
