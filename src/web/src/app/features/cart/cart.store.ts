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
}

const initialState: CartState = {
  items: [],
  loading: false,
  error: null,
  checkoutCorrelationId: null,
};

export const CartStore = signalStore(
  { providedIn: 'root' }, // Global store accessible everywhere
  withState(initialState),

  withComputed((store) => ({
    totalItems: computed(() => store.items().reduce((sum, item) => sum + item.quantity, 0)),
    isEmpty: computed(() => store.items().length === 0),
    totalPrice: computed(() => store.items().reduce((sum, item) => sum + (item.quantity * (item.unitPrice || 0)), 0))
  })),

  withMethods((store, cartService = inject(CartService)) => ({
    async loadCart(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const cart = await cartService.getCart();
        patchState(store, { items: cart.items, loading: false });
      } catch (err: any) {
        patchState(store, { error: 'Failed to load cart', loading: false });
      }
    },

    async addToCart(sku: string, quantity: number = 1, unitPrice?: number): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        // Optimistic local update
        const currentItems = [...store.items()];
        const existingItem = currentItems.find((i) => i.sku === sku);

        if (existingItem) {
          existingItem.quantity += quantity;
          if (unitPrice !== undefined) existingItem.unitPrice = unitPrice;
        } else {
          currentItems.push({ sku, quantity, unitPrice });
        }

        const updatedCart = await cartService.updateCart({ items: currentItems });

        patchState(store, { items: updatedCart.items, loading: false });
      } catch (err: any) {
        patchState(store, { error: 'Failed to add item to cart', loading: false });
        // Re-load cart to discard failed optimistic update
        await this.loadCart();
      }
    },

    async updateQuantity(sku: string, quantity: number): Promise<void> {
      if (quantity <= 0) {
        await this.removeFromCart(sku);
        return;
      }

      patchState(store, { loading: true, error: null });
      try {
        const currentItems = store.items().map((i) => (i.sku === sku ? { ...i, quantity } : i));

        const updatedCart = await cartService.updateCart({ items: currentItems });
        patchState(store, { items: updatedCart.items, loading: false });
      } catch (err: any) {
        patchState(store, { error: 'Failed to update quantity', loading: false });
      }
    },

    async removeFromCart(sku: string): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const currentItems = store.items().filter((i) => i.sku !== sku);

        const updatedCart = await cartService.updateCart({ items: currentItems });
        patchState(store, { items: updatedCart.items, loading: false });
      } catch (err: any) {
        patchState(store, { error: 'Failed to remove item', loading: false });
      }
    },

    async checkout(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const response = await cartService.checkout();
        patchState(store, {
          items: [],
          checkoutCorrelationId: response.correlationId,
          loading: false,
        });
      } catch (err: any) {
        patchState(store, { error: 'Checkout failed', loading: false });
      }
    },
  })),

  withHooks({
    onInit(store) {
      // Load cart data when the application starts
      store.loadCart();
    },
  }),
);
