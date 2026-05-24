import { checkoutTest as test, expect } from '../fixtures/checkout.fixture';
import { createTestData, runCheckoutFlow, addToCart } from '../utils/api-helpers';

test.describe('Full Checkout Flow', () => {
  test('register, add product to cart via API, checkout and pay', async ({
    page,
    playwright,
    cartPage,
    checkoutEnhancedPage,
  }) => {
    // --- Step 1: Create test data environment ---
    const data = await createTestData(playwright.request, {
      productCount: 1,
      stockPerProduct: 50,
    });

    // Copy auth cookies to the browser context
    const storageState = await data.buyerApi.storageState();
    await page.context().addCookies(storageState.cookies);
    await page.goto('/catalog');
    await page.waitForLoadState('domcontentloaded');

    // --- Step 2: Add product to cart via API ---
    await addToCart(data.buyerApi, data.products[0].sku, 1, data.products[0].price, data.store.sellerId);

    // --- Step 3: Navigate to cart and verify item ---
    await cartPage.goto();
    await cartPage.waitForPageLoad();

    const isEmpty = await cartPage.isEmpty();
    expect(isEmpty).toBe(false);

    const cartItem = await cartPage.getCartItem(data.products[0].sku);
    await expect(cartItem).toBeVisible({ timeout: 5000 });

    // --- Step 4: Proceed to checkout ---
    await cartPage.proceedToCheckout();
    await page.waitForLoadState('domcontentloaded');
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
    await page.waitForLoadState('domcontentloaded');

    // --- Step 6: Select express shipping ---
    await checkoutEnhancedPage.selectExpressShipping();
    await expect(checkoutEnhancedPage.continueToPaymentBtn).toBeVisible({ timeout: 10000 });

    // --- Step 7: Continue to payment ---
    await checkoutEnhancedPage.continueToPayment();
    await expect(checkoutEnhancedPage.placeOrderBtn).toBeVisible({ timeout: 10000 });

    // --- Step 8: Place order ---
    await checkoutEnhancedPage.placeOrder();
    await page.waitForLoadState('domcontentloaded');

    // --- Step 9: Verify order was submitted ---
    const submittedVisible = await checkoutEnhancedPage.orderSubmittedHeading.isVisible().catch(() => false);
    const completedVisible = await checkoutEnhancedPage.isCompleted().catch(() => false);
    const faultedVisible = await checkoutEnhancedPage.isFaulted().catch(() => false);

    expect(faultedVisible).toBe(false);
    expect(submittedVisible || completedVisible).toBe(true);

    if (submittedVisible && !completedVisible) {
      await expect(checkoutEnhancedPage.statusCompleted).toBeVisible({ timeout: 35000 });
    }

    // Cleanup
    await data.buyerApi.dispose();
    await data.sellerApi.dispose();
    await data.adminApi.dispose();
  });

  test('full checkout via API helpers (runCheckoutFlow)', async ({ playwright, testProduct, testStore }) => {
    const data = await createTestData(playwright.request, { productCount: 1 });

    const { correlationId, finalOrder } = await runCheckoutFlow(
      data.buyerApi,
      [{ sku: data.products[0].sku, quantity: 1, price: data.products[0].price, shopId: data.store.sellerId }],
      {
        addressLine1: '456 Helper Street',
        city: 'Helper City',
        state: 'NY',
        postalCode: '10001',
        country: 'US',
      }
    );

    expect(correlationId).toBeTruthy();
    expect(finalOrder).not.toBeNull();
    expect(finalOrder!.statusName).toBe('Completed');

    await data.buyerApi.dispose();
    await data.sellerApi.dispose();
    await data.adminApi.dispose();
  });
});
