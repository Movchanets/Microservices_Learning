// CheckoutStore unit tests.
// Tests the checkout submission flow: delegates to CartStore.checkout(), tracks
// submitting/error state, handles empty cart validation and checkout failures.
// Also covers setOrder and reset state management.

import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { CheckoutStore } from './checkout.store';
import { CartStore } from '../cart/cart.store';

describe('CheckoutStore', () => {
  let store: any;

  const mockCartStore = {
    items: signal([{ sku: 'PROD-1', quantity: 2 }]),
    isEmpty: signal(false),
    checkout: vi.fn().mockResolvedValue(undefined),
    checkoutCorrelationId: signal('test-correlation-id'),
    totalPrice: signal(20),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        { provide: CartStore, useValue: mockCartStore },
      ],
    });

    store = TestBed.inject(CheckoutStore);
    store.reset();
    mockCartStore.items.set([{ sku: 'PROD-1', quantity: 2 }]);
    mockCartStore.checkout.mockResolvedValue(undefined);
    vi.clearAllMocks();
  });

  it('should initialize with default state', () => {
    expect(store.submitting()).toBe(false);
    expect(store.error()).toBeNull();
    expect(store.order()).toBeNull();
    expect(store.hasOrder()).toBe(false);
    expect(store.orderStatus()).toBeNull();
  });

  describe('submitCheckout', () => {
    const testAddress = { addressLine1: '123 Main St', city: 'NY', state: 'NY', postalCode: '10001', country: 'US' };

    it('should call cartStore.checkout on submit', async () => {
      store.setAddress(testAddress);
      await store.submitCheckout();

      expect(mockCartStore.checkout).toHaveBeenCalled();
      expect(store.submitting()).toBe(false);
      expect(store.error()).toBeNull();
    });

    it('should set submitting during checkout', async () => {
      store.setAddress(testAddress);
      let resolveCheckout: () => void;
      mockCartStore.checkout.mockReturnValueOnce(
        new Promise<void>((resolve) => { resolveCheckout = resolve; })
      );

      const promise = store.submitCheckout();
      expect(store.submitting()).toBe(true);

      resolveCheckout!();
      await promise;

      expect(store.submitting()).toBe(false);
    });

    it('should set error when cart is empty', async () => {
      store.setAddress(testAddress);
      mockCartStore.items.set([]);

      await store.submitCheckout();

      expect(store.error()).toBe('Cart is empty');
      expect(mockCartStore.checkout).not.toHaveBeenCalled();
    });

    it('should set error when address is missing', async () => {
      await store.submitCheckout();

      expect(store.error()).toBe('Shipping address is required');
      expect(mockCartStore.checkout).not.toHaveBeenCalled();
    });

    it('should set error when checkout fails', async () => {
      store.setAddress(testAddress);
      mockCartStore.checkout.mockRejectedValueOnce(new Error('Network error'));

      await store.submitCheckout();

      expect(store.error()).toBe('Checkout failed. Please try again.');
      expect(store.submitting()).toBe(false);
    });
  });

  describe('setOrder', () => {
    it('should set the order and computed signals', () => {
      const order = { id: 'order-1', status: 'Completed' } as any;

      store.setOrder(order);

      expect(store.order()).toEqual(order);
      expect(store.hasOrder()).toBe(true);
      expect(store.orderStatus()).toBe('Completed');
    });
  });

  describe('reset', () => {
    it('should reset to initial state', () => {
      store.setOrder({ id: 'order-1' } as any);
      store.reset();

      expect(store.order()).toBeNull();
      expect(store.submitting()).toBe(false);
      expect(store.error()).toBeNull();
    });
  });
});
