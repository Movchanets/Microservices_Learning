import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { CatalogService } from '../catalog.service';
import { Product, ProductListItem } from '../catalog.models';
import { InventoryService } from '../../../core/services/inventory.service';
import { RecentlyViewedService } from '../../../core/services/recently-viewed.service';
import { StoreService } from '../../seller-dashboard/store.service';
import { StoreSettings } from '../../seller-dashboard/seller.models';
import { extractHttpError } from '../../../core/utils/http.utils';

interface ProductDetailState {
  product: Product | null;
  storeInfo: StoreSettings | null;
  loading: boolean;
  error: string | null;

  stockQuantity: number | null;
  stockLoading: boolean;

  recommendations: ProductListItem[];
  recommendationsLoading: boolean;
}

const initialState: ProductDetailState = {
  product: null,
  storeInfo: null,
  loading: false,
  error: null,

  stockQuantity: null,
  stockLoading: false,

  recommendations: [],
  recommendationsLoading: false,
};

export const ProductDetailStore = signalStore(
  // Feature-scoped — provided in the component
  withState(initialState),
  withMethods((
    store,
    catalogService = inject(CatalogService),
    inventoryService = inject(InventoryService),
    recentlyViewedService = inject(RecentlyViewedService),
    storeService = inject(StoreService),
  ) => ({

    async loadProduct(id: string): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const product = await catalogService.getProduct(id);
        patchState(store, { product });
        recentlyViewedService.trackView(product.id);

        // Fire-and-forget secondary data loads
        const firstSkuCode = product.skus?.[0]?.skuCode;
        if (firstSkuCode) {
          this.loadStock(firstSkuCode);
        }
        this.loadRecommendations(product.id);
        this.loadStoreInfo(product.storeId);
      } catch (err: unknown) {
        patchState(store, {
          error: extractHttpError(err, 'Failed to load product details'),
        });
      } finally {
        patchState(store, { loading: false });
      }
    },

    async loadStock(sku: string): Promise<void> {
      patchState(store, { stockLoading: true });
      try {
        const item = await inventoryService.checkStock(sku);
        patchState(store, { stockQuantity: item.availableQuantity });
      } catch {
        patchState(store, { stockQuantity: null });
      } finally {
        patchState(store, { stockLoading: false });
      }
    },

    async loadRecommendations(productId: string): Promise<void> {
      patchState(store, { recommendationsLoading: true });
      try {
        const items = await catalogService.getRecommendations(productId);
        patchState(store, { recommendations: items });
      } catch {
        patchState(store, { recommendations: [] });
      } finally {
        patchState(store, { recommendationsLoading: false });
      }
    },

    async loadStoreInfo(storeId: string): Promise<void> {
      try {
        const info = await storeService.getStoreById(storeId);
        patchState(store, { storeInfo: info });
      } catch {
        patchState(store, { storeInfo: null });
      }
    },
  })),
);
