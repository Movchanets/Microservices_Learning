// Seller product store.
// NgRx SignalStore managing seller product CRUD operations.
// Loads products filtered by sellerId, handles create/update/delete/activate/deactivate.

import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { SellerProductService } from './seller-product.service';
import { SellerProduct, CreateProductRequest, UpdateProductRequest } from './seller.models';
import { StoreSettingsStore } from './store-settings.store';

interface SellerProductState {
  products: SellerProduct[];
  selectedProduct: SellerProduct | null;
  loading: boolean;
  error: string | null;
}

const initialState: SellerProductState = {
  products: [],
  selectedProduct: null,
  loading: false,
  error: null,
};

export const SellerProductStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed((store) => ({
    activeProducts: computed(() => store.products().filter(p => p.status === 'Active')),
    inactiveProducts: computed(() => store.products().filter(p => p.status !== 'Active')),
    hasProducts: computed(() => store.products().length > 0),
  })),

  withMethods((store, productService = inject(SellerProductService), storeSettingsStore = inject(StoreSettingsStore)) => ({

    async loadProducts(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const storeId = storeSettingsStore.storeId() ?? '';
        if (!storeId) {
          patchState(store, { products: [], loading: false });
          return;
        }
        const products = await productService.getMyProducts(storeId);
        patchState(store, { products, loading: false });
      } catch {
        patchState(store, { error: 'Failed to load products', loading: false });
      }
    },

    async loadProductById(id: string): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const product = await productService.getProductById(id);
        patchState(store, { selectedProduct: product, loading: false });
      } catch {
        patchState(store, { error: 'Failed to load product', loading: false });
      }
    },

    async createProduct(request: CreateProductRequest): Promise<boolean> {
      patchState(store, { loading: true, error: null });
      try {
        const product = await productService.createProduct(request);
        patchState(store, {
          products: [...store.products(), product],
          loading: false,
        });
        return true;
      } catch {
        patchState(store, { error: 'Failed to create product', loading: false });
        return false;
      }
    },

    async updateProduct(id: string, request: UpdateProductRequest, newPrice?: number, currency?: string): Promise<boolean> {
      patchState(store, { loading: true, error: null });
      try {
        const updated = await productService.updateProduct(id, request);
        // If price changed, call the separate price change endpoint
        if (newPrice !== undefined && currency) {
          await productService.changePrice(id, newPrice, currency);
        }
        // Reload to get fresh data including price change
        const fresh = newPrice !== undefined ? await productService.getProductById(id) : updated;
        patchState(store, {
          products: store.products().map(p => p.id === id ? fresh : p),
          selectedProduct: store.selectedProduct()?.id === id ? fresh : store.selectedProduct(),
          loading: false,
        });
        return true;
      } catch {
        patchState(store, { error: 'Failed to update product', loading: false });
        return false;
      }
    },

    async activateProduct(id: string): Promise<boolean> {
      patchState(store, { error: null });
      try {
        await productService.activateProduct(id);
        patchState(store, {
          products: store.products().map(p => p.id === id ? { ...p, status: 'Active' as const } : p),
        });
        return true;
      } catch {
        patchState(store, { error: 'Failed to activate product' });
        return false;
      }
    },

    async deactivateProduct(id: string): Promise<boolean> {
      patchState(store, { error: null });
      try {
        await productService.deactivateProduct(id);
        patchState(store, {
          products: store.products().map(p => p.id === id ? { ...p, status: 'Inactive' as const } : p),
        });
        return true;
      } catch {
        patchState(store, { error: 'Failed to deactivate product' });
        return false;
      }
    },

    async deleteProduct(id: string): Promise<boolean> {
      patchState(store, { loading: true, error: null });
      try {
        await productService.deleteProduct(id);
        patchState(store, {
          products: store.products().filter(p => p.id !== id),
          loading: false,
        });
        return true;
      } catch {
        patchState(store, { error: 'Failed to delete product', loading: false });
        return false;
      }
    },

    clearSelected(): void {
      patchState(store, { selectedProduct: null });
    },
  }))
);
