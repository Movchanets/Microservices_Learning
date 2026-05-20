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
import { CartItem } from './cart.models';

interface CartState {
  items: CartItem[];
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
  { providedIn: 'root' }, // Global store accessible everywhere
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
      } catch (err: any) {
        patchState(store, { error: 'Failed to load cart', loading: false });
      }
    },

    async addToCart(sku: string, quantity: number = 1, shopId?: string): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const updatedCart = await cartService.addItem(sku, quantity, shopId);
        patchState(store, { items: updatedCart.items, loading: false, isDrawerOpen: true });
      } catch (err: any) {
        patchState(store, { error: 'Failed to add item to cart', loading: false });
        await this.loadCart();
      }
    },

    async updateQuantity(sku: string, quantity: number, shopId?: string): Promise<void> {
      if (quantity <= 0) {
        await this.removeFromCart(sku, shopId);
        return;
      }

      patchState(store, { loading: true, error: null });
      try {
        const updatedCart = await cartService.updateItem(sku, quantity, shopId);
        patchState(store, { items: updatedCart.items, loading: false });
      } catch (err: any) {
        patchState(store, { error: 'Failed to update quantity', loading: false });
        await this.loadCart();
      }
    },

    async removeFromCart(sku: string, shopId?: string): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const updatedCart = await cartService.removeItem(sku, shopId);
        patchState(store, { items: updatedCart.items, loading: false });
      } catch (err: any) {
        patchState(store, { error: 'Failed to remove item', loading: false });
        await this.loadCart();
      }
    },

    async checkout(address?: any): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const response = await cartService.checkout(address);
        patchState(store, {
          items: [],
          checkoutCorrelationId: response.correlationId,
          loading: false,
          isDrawerOpen: false,
        });
      } catch (err: any) {
        patchState(store, { error: 'Checkout failed', loading: false });
      }
    },
  })),

  withHooks({
    onInit(store) {
      const platformId = inject(PLATFORM_ID);
      if (isPlatformBrowser(platformId)) {
        // Load cart data when the application starts in browser
        store.loadCart();
      }
    },
  }),
);
