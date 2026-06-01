import { inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { computed } from '@angular/core';
import { CatalogService } from '../catalog.service';
import { Product, ProductListItem, Sku, VariantMatrix } from '../catalog.models';
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

  // Variant matrix
  variantMatrix: VariantMatrix | null;
  variantMatrixLoading: boolean;
  selectedVariants: Record<string, string>;
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

  variantMatrix: null,
  variantMatrixLoading: false,
  selectedVariants: {},
};

export const ProductDetailStore = signalStore(
  // Feature-scoped — provided in the component
  withState(initialState),
  withComputed((store) => ({
    /**
     * Whether the product has variant-axis attributes (shows variant picker vs flat SKU list).
     */
    hasVariantPicker: computed(() => {
      const matrix = store.variantMatrix();
      return matrix !== null && matrix.axes.length > 0;
    }),

    /**
     * The currently selected SKU.
     * If variant picker is active, resolves from variant selections.
     * If no variant picker, returns null (component handles legacy fallback).
     */
    selectedVariantSku: computed((): Sku | null => {
      const matrix = store.variantMatrix();
      const product = store.product();
      if (!matrix || !product?.skus?.length) return null;

      const selected = store.selectedVariants();
      const allAxesSelected = matrix.axes.every(
        axis => selected[axis.key] !== undefined
      );
      if (!allAxesSelected) return null;

      const match = matrix.options.find(option =>
        matrix.axes.every(axis =>
          option.combination[axis.key]?.toLowerCase() === selected[axis.key]?.toLowerCase()
        )
      );

      if (!match?.skuId) return null;
      return product.skus.find(s => s.id === match.skuId) ?? null;
    }),
  })),
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

    async loadVariantMatrix(productId: string): Promise<void> {
      patchState(store, { variantMatrixLoading: true });
      try {
        const matrix = await catalogService.getVariantMatrix(productId);
        patchState(store, { variantMatrix: matrix });
      } catch {
        patchState(store, { variantMatrix: null });
      } finally {
        patchState(store, { variantMatrixLoading: false });
      }
    },

    /**
     * Selects a value for a specific variant axis.
     * Updates state, then loads stock for the matched SKU if all axes are selected.
     */
    selectVariant(axisKey: string, value: string): void {
      const current = store.selectedVariants();
      const updated = { ...current, [axisKey]: value };
      patchState(store, { selectedVariants: updated });

      // After state update, the selectedVariantSku computed re-evaluates.
      // Load stock for the matched SKU if available.
      const sku = store.selectedVariantSku();
      if (sku) {
        this.loadStock(sku.skuCode);
      }
    },
  })),
);
