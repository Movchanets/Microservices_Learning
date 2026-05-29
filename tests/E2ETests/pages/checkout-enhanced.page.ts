import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page object for `/checkout` — enhanced checkout with payment options.
 */
export class CheckoutEnhancedPage extends BasePage {
  readonly pageHeading: Locator;
  readonly placeOrderBtn: Locator;
  readonly backToCartLink: Locator;
  readonly emptyCartMessage: Locator;

  // Address form
  readonly addressLine1Input: Locator;
  readonly addressLine2Input: Locator;
  readonly cityInput: Locator;
  readonly stateInput: Locator;
  readonly postalCodeInput: Locator;
  readonly countryInput: Locator;
  readonly addressSaveBtn: Locator;

  // Shipping method
  readonly standardShippingRadio: Locator;
  readonly expressShippingRadio: Locator;

  // Payment
  readonly continueToPaymentBtn: Locator;

  // Submission
  readonly orderSubmittedHeading: Locator;
  readonly correlationIdText: Locator;

  // Status
  readonly statusProcessing: Locator;
  readonly statusCompleted: Locator;
  readonly statusCancelled: Locator;
  readonly statusFaulted: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByTestId('checkout-heading');
    this.placeOrderBtn = page.getByTestId('checkout-place-order');
    this.backToCartLink = page.getByTestId('checkout-back-cart');
    this.emptyCartMessage = page.getByTestId('checkout-empty');

    // Address form
    this.addressLine1Input = page.getByTestId('address-line1');
    this.addressLine2Input = page.getByTestId('address-line2');
    this.cityInput = page.getByTestId('address-city');
    this.stateInput = page.getByTestId('address-state');
    this.postalCodeInput = page.getByTestId('address-postal-code');
    this.countryInput = page.getByTestId('address-country');
    this.addressSaveBtn = page.getByTestId('address-save-btn');

    // Shipping method
    this.standardShippingRadio = page.getByTestId('checkout-shipping-standard');
    this.expressShippingRadio = page.getByTestId('checkout-shipping-express');

    // Payment
    this.continueToPaymentBtn = page.getByTestId('checkout-continue-payment');

    // Submission
    this.orderSubmittedHeading = page.getByTestId('checkout-order-submitted');
    this.correlationIdText = page.getByTestId('checkout-correlation-id');

    // Status
    this.statusProcessing = page.getByTestId('checkout-status-processing');
    this.statusCompleted = page.getByTestId('checkout-status-completed');
    this.statusCancelled = page.getByTestId('checkout-status-cancelled');
    this.statusFaulted = page.getByTestId('checkout-status-faulted');
  }

  async goto() {
    await this.page.goto('/checkout');
  }

  async fillAddress(address: {
    line1: string;
    line2?: string;
    city: string;
    state: string;
    postalCode: string;
    country?: string;
  }) {
    await this.addressLine1Input.fill(address.line1);
    if (address.line2) {
      await this.addressLine2Input.fill(address.line2);
    }
    await this.cityInput.fill(address.city);
    await this.stateInput.fill(address.state);
    await this.postalCodeInput.fill(address.postalCode);
    if (address.country) {
      await this.countryInput.selectOption(address.country);
    }
  }

  async saveAddress() {
    await this.addressSaveBtn.click();
  }

  async selectStandardShipping() {
    await this.standardShippingRadio.click();
  }

  async selectExpressShipping() {
    await this.expressShippingRadio.click();
  }

  async continueToPayment() {
    await this.continueToPaymentBtn.click();
  }

  async placeOrder() {
    await this.placeOrderBtn.click();
  }

  async getCorrelationId(): Promise<string> {
    return this.correlationIdText.innerText();
  }

  async isSubmitted(): Promise<boolean> {
    return this.orderSubmittedHeading.isVisible();
  }

  async isProcessing(): Promise<boolean> {
    return this.statusProcessing.isVisible();
  }

  async isCompleted(): Promise<boolean> {
    return this.statusCompleted.isVisible();
  }

  async isFaulted(): Promise<boolean> {
    return this.statusFaulted.isVisible();
  }

  async isEmpty(): Promise<boolean> {
    return this.emptyCartMessage.isVisible();
  }
}
