import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { CartStore } from '../cart/cart.store';
import { Order } from './checkout.models';

interface CheckoutState {
  submitting: boolean;
  error: string | null;
  order: Order | null;
}

const initialState: CheckoutState = {
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
    async submitCheckout(): Promise<void> {
      const items = cartStore.items();

      if (items.length === 0) {
        patchState(store, { error: 'Cart is empty' });
        return;
      }

      patchState(store, { submitting: true, error: null });

      try {
        await cartStore.checkout();
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
