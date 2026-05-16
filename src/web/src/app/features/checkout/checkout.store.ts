import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { CartStore } from '../cart/cart.store';
import { Order, Address } from './checkout.models';

interface CheckoutState {
  address: Address | null;
  shippingMethod: 'standard' | 'express';
  submitting: boolean;
  error: string | null;
  order: Order | null;
}

const initialState: CheckoutState = {
  address: null,
  shippingMethod: 'standard',
  submitting: false,
  error: null,
  order: null,
};

export const CheckoutStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed((store) => ({
    hasOrder: computed(() => store.order() !== null),
    orderStatus: computed(() => store.order()?.status ?? null),
  })),

  withMethods((store, cartStore = inject(CartStore)) => ({
    setAddress(address: Address): void {
      patchState(store, { address });
    },

    setShippingMethod(method: 'standard' | 'express'): void {
      patchState(store, { shippingMethod: method });
    },

    async submitCheckout(): Promise<void> {
      const items = cartStore.items();
      const address = store.address();

      if (items.length === 0) {
        patchState(store, { error: 'Cart is empty' });
        return;
      }

      if (!address) {
        patchState(store, { error: 'Shipping address is required' });
        return;
      }

      patchState(store, { submitting: true, error: null });

      try {
        await cartStore.checkout(address);
        patchState(store, { submitting: false });
      } catch {
        patchState(store, {
          error: 'Checkout failed. Please try again.',
          submitting: false,
        });
      }
    },

    setOrder(order: Order): void {
      patchState(store, { order });
    },

    reset(): void {
      patchState(store, initialState);
    },
  })),
);
