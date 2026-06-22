/**
 * Seller product store.
 *
 * NgRx SignalStore managing seller product CRUD operations.
 * Loads products filtered by storeId, handles create/update/delete/activate/deactivate.
 */

import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { SellerProductService } from './seller-product.service';
import { SellerProduct, CreateProductRequest, UpdateProductRequest, AddSkuRequest, BulkAddSkuRequest, BulkAddSkuResult } from './seller.models';
import { Sku } from '../catalog/catalog.models';
import { StoreSettingsStore } from './store-settings.store';

// ── State ──────────────────────────────────────────────────

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

// ── Store ──────────────────────────────────────────────────

export const SellerProductStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed((store) => ({
    activeProducts: computed(() => store.products().filter(p => p.status === 'Active')),
    inactiveProducts: computed(() => store.products().filter(p => p.status !== 'Active')),
    hasProducts: computed(() => store.products().length > 0),
  })),

  withMethods((store, productService = inject(SellerProductService), storeSettingsStore = inject(StoreSettingsStore)) => {

    // ── Helpers ────────────────────────────────────────────

    /** Apply a transformation to the product matching `id` in both products[] and selectedProduct. */
    function updateProductInState(id: string, updater: (p: SellerProduct) => SellerProduct): void {
      patchState(store, {
        products: store.products().map(p => p.id === id ? updater(p) : p),
        selectedProduct: store.selectedProduct()?.id === id
          ? updater(store.selectedProduct()!)
          : store.selectedProduct(),
      });
    }

    // ── Methods ────────────────────────────────────────────

    return {

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

      async createProduct(request: CreateProductRequest): Promise<SellerProduct | null> {
        patchState(store, { loading: true, error: null });
        try {
          const product = await productService.createProduct(request);
          patchState(store, {
            products: [...store.products(), product],
            loading: false,
          });
          return product;
        } catch {
          patchState(store, { error: 'Failed to create product', loading: false });
          return null;
        }
      },

      async updateProduct(id: string, request: UpdateProductRequest): Promise<boolean> {
        patchState(store, { loading: true, error: null });
        try {
          const updated = await productService.updateProduct(id, request);
          updateProductInState(id, () => updated);
          patchState(store, { loading: false });
          return true;
        } catch {
          patchState(store, { error: 'Failed to update product', loading: false });
          return false;
        }
      },

      async addSku(productId: string, request: AddSkuRequest): Promise<Sku | null> {
        try {
          const sku = await productService.addSku(productId, request);
          updateProductInState(productId, p => ({ ...p, skus: [...p.skus, sku] }));
          return sku;
        } catch {
          patchState(store, { error: 'Failed to add SKU' });
          return null;
        }
      },

      async bulkAddSku(productId: string, request: BulkAddSkuRequest): Promise<BulkAddSkuResult | null> {
        patchState(store, { loading: true, error: null });
        try {
          const result = await productService.bulkAddSku(productId, request);
          // Reload the product to get all SKUs with proper IDs
          const product = await productService.getProductById(productId);
          updateProductInState(productId, () => product);
          patchState(store, { loading: false });
          return result;
        } catch {
          patchState(store, { error: 'Failed to bulk add SKUs', loading: false });
          return null;
        }
      },

      async removeSku(productId: string, skuId: string): Promise<boolean> {
        try {
          await productService.removeSku(productId, skuId);
          updateProductInState(productId, p => ({ ...p, skus: p.skus.filter(s => s.id !== skuId) }));
          return true;
        } catch {
          patchState(store, { error: 'Failed to remove SKU' });
          return false;
        }
      },

      async activateProduct(id: string): Promise<boolean> {
        patchState(store, { error: null });
        try {
          await productService.activateProduct(id);
          updateProductInState(id, p => ({ ...p, status: 'Active' as const }));
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
          updateProductInState(id, p => ({ ...p, status: 'Inactive' as const }));
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
    };
  })
);
