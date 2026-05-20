// Seller product store.
// NgRx SignalStore managing seller product CRUD operations.
// Loads products filtered by sellerId, handles create/update/delete.

import { computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { SellerProductService } from './seller-product.service';
import { SellerProduct, CreateProductRequest, UpdateProductRequest } from './seller.models';

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

  withMethods((store, productService = inject(SellerProductService), platformId = inject(PLATFORM_ID)) => ({

    async loadProducts(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const storeId = isPlatformBrowser(platformId) ? (localStorage.getItem('storeId') || '') : '';
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

    async updateProduct(id: string, request: UpdateProductRequest): Promise<boolean> {
      patchState(store, { loading: true, error: null });
      try {
        const updated = await productService.updateProduct(id, request);
        patchState(store, {
          products: store.products().map(p => p.id === id ? updated : p),
          selectedProduct: store.selectedProduct()?.id === id ? updated : store.selectedProduct(),
          loading: false,
        });
        return true;
      } catch {
        patchState(store, { error: 'Failed to update product', loading: false });
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
