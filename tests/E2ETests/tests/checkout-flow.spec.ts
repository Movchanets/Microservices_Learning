import { checkoutTest as test, expect } from '../fixtures/checkout.fixture';
import { registerApi } from '../utils/api-helpers';

test.describe('Full Checkout Flow', () => {
  test('register, add product to cart via API, checkout and pay', async ({
    page,
    playwright,
    cartPage,
    checkoutEnhancedPage,
    testProduct,
    addItemToCart,
  }) => {
    // --- Step 1: Register a fresh user via API ---
    const randomId = Math.random().toString(36).substring(7);
    const email = `checkout_${randomId}@test.com`;
    const password = 'P@ssw0rd123!';

    const buyerApi = await registerApi(
      playwright.request,
      'Checkout',
      'Tester',
      email,
      password
    );

    // Copy auth cookies to the browser context so the page is logged in
    const storageState = await buyerApi.storageState();
    await page.context().addCookies(storageState.cookies);

    // Navigate to catalog to establish the session
    await page.goto('/catalog');
    await page.waitForLoadState('networkidle');

    // --- Step 2: Add product to cart via API ---
    await addItemToCart(buyerApi, 1);

    // --- Step 3: Navigate to cart and verify item ---
    await cartPage.goto();
    await cartPage.waitForPageLoad();

    const isEmpty = await cartPage.isEmpty();
    expect(isEmpty).toBe(false);

    const cartItem = await cartPage.getCartItem(testProduct.sku);
    await expect(cartItem).toBeVisible({ timeout: 5000 });

    // --- Step 4: Proceed to checkout ---
    await cartPage.proceedToCheckout();
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/checkout/);

    // --- Step 5: Fill shipping address and save ---
    await checkoutEnhancedPage.fillAddress({
      line1: '123 Test Street',
      city: 'Testville',
      state: 'CA',
      postalCode: '90210',
      country: 'US',
    });
    await checkoutEnhancedPage.saveAddress();
    await page.waitForLoadState('networkidle');

    // --- Step 6: Select express shipping (to trigger change event) ---
    await checkoutEnhancedPage.selectExpressShipping();

    // Wait for accordion to advance to summary section
    await expect(checkoutEnhancedPage.continueToPaymentBtn).toBeVisible({ timeout: 10000 });

    // --- Step 7: Continue to payment ---
    await checkoutEnhancedPage.continueToPayment();
    await expect(checkoutEnhancedPage.placeOrderBtn).toBeVisible({ timeout: 10000 });

    // --- Step 8: Place order ---
    await checkoutEnhancedPage.placeOrder();

    // --- Step 9: Verify order was submitted ---
    await page.waitForLoadState('networkidle');

    const submittedVisible = await checkoutEnhancedPage.orderSubmittedHeading.isVisible().catch(() => false);
    const completedVisible = await checkoutEnhancedPage.isCompleted().catch(() => false);
    const faultedVisible = await checkoutEnhancedPage.isFaulted().catch(() => false);

    // The order should NOT be faulted (the TotalAmount bug is fixed)
    expect(faultedVisible).toBe(false);

    // The order should be either submitted (processing) or completed
    expect(submittedVisible || completedVisible).toBe(true);

    // Wait for the order to complete (polling takes up to 30s)
    if (submittedVisible && !completedVisible) {
      await expect(checkoutEnhancedPage.statusCompleted).toBeVisible({ timeout: 35000 });
    }

    await buyerApi.dispose();
  });
});
