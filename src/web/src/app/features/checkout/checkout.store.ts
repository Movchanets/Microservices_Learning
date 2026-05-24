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
  submitted: boolean;
  pollingExpired: boolean;
}

const initialState: CheckoutState = {
  address: null,
  shippingMethod: 'standard',
  submitting: false,
  error: null,
  order: null,
  submitted: false,
  pollingExpired: false,
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

      console.log('[CheckoutStore] submitCheckout called', {
        itemCount: items.length,
        hasAddress: !!address,
      });

      if (items.length === 0) {
        console.warn('[CheckoutStore] submitCheckout → aborted: cart empty');
        patchState(store, { error: 'Cart is empty' });
        return;
      }

      if (!address) {
        console.warn('[CheckoutStore] submitCheckout → aborted: no address');
        patchState(store, { error: 'Shipping address is required' });
        return;
      }

      // Capture total before checkout clears the cart items
      const totalAmount = cartStore.totalPrice();
      console.log('[CheckoutStore] submitCheckout → captured totalAmount', { totalAmount });

      patchState(store, { submitting: true, error: null, submitted: true });

      try {
        console.log('[CheckoutStore] submitCheckout → calling cartStore.checkout()', {
          addressLine1: address.addressLine1,
          city: address.city,
        });

        // Real backend call — POST /api/cart/checkout → publishes OrderSubmittedEvent → saga
        await cartStore.checkout({
          addressLine1: address.addressLine1,
          city: address.city,
          state: address.state,
          postalCode: address.postalCode,
          country: address.country,
        });

        const correlationId = cartStore.checkoutCorrelationId();
        console.log('[CheckoutStore] submitCheckout → backend responded', {
          correlationId,
        });

        // Set optimistic order with "Submitted" status.
        // The real orderId + progressive status updates arrive via SignalR/polling.
        patchState(store, {
          order: {
            id: correlationId ?? crypto.randomUUID(),
            buyerId: '',
            status: 'Submitted',
            totalAmount,
            createdAt: new Date().toISOString(),
            completedAt: null,
            items: [],
          },
          submitting: false,
        });

        console.log('[CheckoutStore] submitCheckout → optimistic order set', {
          orderId: store.order()?.id,
          status: 'Submitted',
        });
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Checkout failed. Please try again.';
        console.error('[CheckoutStore] submitCheckout → FAILED', { message, err });
        patchState(store, {
          error: message,
          submitting: false,
          submitted: false,
        });
      }
    },

    setOrder(order: Order): void {
      patchState(store, { order });
    },

    setPollingExpired(expired: boolean): void {
      patchState(store, { pollingExpired: expired });
    },

    /**
     * Called when a terminal failure status (Cancelled/Faulted) is received
     * via SignalR or polling. Sets the error message so the template can
     * surface the failure reason and show a retry mechanism.
     */
    markTerminalFailure(reason: string | null): void {
      patchState(store, {
        submitting: false,
        error: reason || 'Order could not be completed. Please try again.',
      });
    },

    retryCheckout(): void {
      patchState(store, {
        submitted: false,
        pollingExpired: false,
        error: null,
        order: null,
        submitting: false,
      });
    },

    reset(): void {
      patchState(store, initialState);
    },
  })),
);
