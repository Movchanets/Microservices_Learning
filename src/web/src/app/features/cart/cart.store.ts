import { computed, inject } from '@angular/core';
import {
  patchState,
  signalStore,
  withComputed,
  withMethods,
  withState,
  withHooks,
} from '@ngrx/signals';
import { CartService } from './cart.service';
import { CartItemDetails } from './cart.models';

interface CartState {
  items: CartItemDetails[];
  loading: boolean;
  error: string | null;
  checkoutCorrelationId: string | null;
  isDrawerOpen: boolean;
}

const initialState: CartState = {
  items: [],
  loading: false,
  error: null,
  checkoutCorrelationId: null,
  isDrawerOpen: false,
};

import { isPlatformBrowser } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';

export const CartStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed((store) => ({
    totalItems: computed(() => store.items().reduce((sum, item) => sum + item.quantity, 0)),
    isEmpty: computed(() => store.items().length === 0),
    totalPrice: computed(() => store.items().reduce((sum, item) => sum + item.lineTotal, 0))
  })),

  withMethods((store, cartService = inject(CartService)) => ({
    showDrawer() {
      patchState(store, { isDrawerOpen: true });
    },
    hideDrawer() {
      patchState(store, { isDrawerOpen: false });
    },
    toggleDrawer() {
      patchState(store, { isDrawerOpen: !store.isDrawerOpen() });
    },

    async loadCart(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const cart = await cartService.getCart();
        patchState(store, { items: cart.items, loading: false });
      } catch (err: unknown) {
        patchState(store, { error: 'Failed to load cart', loading: false });
      }
    },

    async addToCart(productId: string, quantity: number = 1): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        await cartService.addItem(productId, quantity);
        // Re-fetch enriched cart from BFF (mutation response lacks product details)
        await this.loadCart();
        patchState(store, { isDrawerOpen: true });
      } catch (err: unknown) {
        patchState(store, { error: 'Failed to add item to cart', loading: false });
      }
    },

    async updateQuantity(productId: string, quantity: number): Promise<void> {
      if (quantity <= 0) {
        await this.removeFromCart(productId);
        return;
      }

      patchState(store, { loading: true, error: null });
      try {
        await cartService.updateItem(productId, quantity);
        // Re-fetch enriched cart from BFF
        await this.loadCart();
      } catch (err: unknown) {
        patchState(store, { error: 'Failed to update quantity', loading: false });
      }
    },

    async removeFromCart(productId: string): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        await cartService.removeItem(productId);
        // Re-fetch enriched cart from BFF
        await this.loadCart();
      } catch (err: unknown) {
        patchState(store, { error: 'Failed to remove item', loading: false });
      }
    },

    async checkout(address?: { addressLine1: string; city: string; state: string; postalCode: string; country: string }): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const response = await cartService.checkout(address);
        patchState(store, {
          items: [],
          checkoutCorrelationId: response.correlationId,
          loading: false,
          isDrawerOpen: false,
        });
      } catch (err: unknown) {
        patchState(store, { error: 'Checkout failed', loading: false });
      }
    },
  })),

  withHooks({
    onInit(store) {
      const platformId = inject(PLATFORM_ID);
      if (isPlatformBrowser(platformId)) {
        store.loadCart();
      }
    },
  }),
);
