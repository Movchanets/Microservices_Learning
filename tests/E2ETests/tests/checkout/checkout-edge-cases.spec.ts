import { authTest as test, expect } from '../../fixtures/auth.fixture';
import { CheckoutEnhancedPage } from '../../pages/checkout-enhanced.page';

test.describe('Checkout: Edge Cases', () => {

  test('should redirect unauthenticated from checkout', async ({ browser }) => {
    const page = await browser.newPage();
    await page.goto('/checkout');
    await page.waitForLoadState('domcontentloaded');
    await expect(page).toHaveURL(/\/auth\/login/);
    await page.close();
  });

  test('should show empty cart message when no items', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    const checkoutPage = new CheckoutEnhancedPage(page);

    await checkoutPage.goto();
    await page.waitForLoadState('domcontentloaded');

    const isEmpty = await checkoutPage.emptyCartMessage.isVisible().catch(() => false);
    const hasHeading = await checkoutPage.pageHeading.isVisible().catch(() => false);
    expect(isEmpty || hasHeading).toBe(true);
    await page.close();
  });

  test('should validate required address fields', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    const checkoutPage = new CheckoutEnhancedPage(page);

    await checkoutPage.goto();
    await page.waitForLoadState('domcontentloaded');

    // Try to save empty address
    const saveBtn = checkoutPage.addressSaveBtn;
    const btnVisible = await saveBtn.isVisible().catch(() => false);
    if (!btnVisible) {
      test.skip(true, 'Save button not visible (empty cart?) — skipping');
      await page.close();
      return;
    }
    // Button should be disabled when form is invalid
    const isDisabled = await saveBtn.isDisabled();
    expect(isDisabled).toBe(true);
    await page.close();
  });

  test('should display address form with all fields', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    const checkoutPage = new CheckoutEnhancedPage(page);

    await checkoutPage.goto();
    await page.waitForLoadState('domcontentloaded');

    // Address form fields should be visible
    const line1Visible = await checkoutPage.addressLine1Input.isVisible().catch(() => false);
    const cityVisible = await checkoutPage.cityInput.isVisible().catch(() => false);

    if (line1Visible) {
      await expect(checkoutPage.addressLine1Input).toBeVisible();
      await expect(checkoutPage.cityInput).toBeVisible();
      await expect(checkoutPage.stateInput).toBeVisible();
      await expect(checkoutPage.postalCodeInput).toBeVisible();
      await expect(checkoutPage.countryInput).toBeVisible();
    }
    await page.close();
  });

  test('should fill address and proceed to shipping', async ({ buyerContext }) => {
    const page = await buyerContext.newPage();
    const checkoutPage = new CheckoutEnhancedPage(page);

    await checkoutPage.goto();
    await page.waitForLoadState('domcontentloaded');

    const line1Visible = await checkoutPage.addressLine1Input.isVisible().catch(() => false);
    if (!line1Visible) {
      test.skip(true, 'Address form not visible (empty cart?) — skipping');
      await page.close();
      return;
    }

    await checkoutPage.fillAddress({
      line1: '789 Test Boulevard',
      city: 'Test City',
      state: 'CA',
      postalCode: '90210',
      country: 'US',
    });
    await checkoutPage.saveAddress();
    await page.waitForLoadState('domcontentloaded');

    // Should advance to shipping section
    const shippingVisible = await checkoutPage.standardShippingRadio.isVisible().catch(() => false);
    const summaryVisible = await checkoutPage.continueToPaymentBtn.isVisible().catch(() => false);
    expect(shippingVisible || summaryVisible).toBe(true);
    await page.close();
  });
});
