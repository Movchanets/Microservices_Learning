import { inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { computed } from '@angular/core';
import { CatalogService } from '../catalog.service';
import { GalleryItem, Product, Sku, VariantMatrix } from '../catalog.models';
import { InventoryService } from '../../../core/services/inventory.service';
import { MediaService } from '../../../core/services/media.service';
import { RecentlyViewedService } from '../../../core/services/recently-viewed.service';
import { StoreService } from '../../seller-dashboard/store.service';
import { StoreSettings } from '../../seller-dashboard/seller.models';
import { extractHttpError } from '../../../core/utils/http.utils';

export interface SpecEntry {
  key: string;
  displayName: string;
  value: string;
}

function humanizeKey(key: string): string {
  return key.charAt(0).toUpperCase() + key.slice(1).replace(/([A-Z])/g, ' $1');
}

interface ProductDetailState {
  product: Product | null;
  storeInfo: StoreSettings | null;
  loading: boolean;
  error: string | null;

  stockQuantity: number | null;
  stockLoading: boolean;

  // Variant matrix
  variantMatrix: VariantMatrix | null;
  variantMatrixLoading: boolean;
  selectedVariants: Record<string, string>;

  // Gallery
  productGallery: GalleryItem[];
  skuGallery: GalleryItem[];
  galleryLoading: boolean;
}

const initialState: ProductDetailState = {
  product: null,
  storeInfo: null,
  loading: false,
  error: null,

  stockQuantity: null,
  stockLoading: false,

  variantMatrix: null,
  variantMatrixLoading: false,
  selectedVariants: {},

  productGallery: [],
  skuGallery: [],
  galleryLoading: false,
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

    /**
     * Merged gallery: SKU gallery takes priority, product gallery as fallback.
     * When the buyer selects a variant with its own gallery, those images
     * replace the product-level images. When no SKU gallery exists,
     * product-level images are shown.
     */
    mergedGallery: computed(() => {
      const sku = store.skuGallery();
      if (sku.length > 0) return sku;
      return store.productGallery();
    }),

    /**
     * Spec entries from the selected SKU's typedAttributes,
     * excluding variant axis keys (already shown in variant picker).
     * Returns key-value pairs with humanized display names.
     */
    specEntries: computed((): SpecEntry[] => {
      const product = store.product();
      const matrix = store.variantMatrix();
      if (!product?.skus?.length || !matrix) return [];

      // Resolve the selected SKU
      const selected = store.selectedVariants();
      const allAxesSelected = matrix.axes.every(
        axis => selected[axis.key] !== undefined
      );

      let sku: Sku | undefined;
      if (allAxesSelected) {
        const match = matrix.options.find(option =>
          matrix.axes.every(axis =>
            option.combination[axis.key]?.toLowerCase() === selected[axis.key]?.toLowerCase()
          )
        );
        if (match?.skuId) {
          sku = product.skus.find(s => s.id === match.skuId);
        }
      }
      if (!sku) sku = product.skus[0];
      if (!sku) return [];

      // Collect axis keys to exclude
      const axisKeys = new Set(matrix.axes.map(a => a.key));

      // Filter typedAttributes, exclude axis keys
      return Object.entries(sku.typedAttributes)
        .filter(([key]) => !axisKeys.has(key))
        .map(([key, value]) => ({
          key,
          displayName: humanizeKey(key),
          value,
        }));
    }),
  })),
  withMethods((
    store,
    catalogService = inject(CatalogService),
    mediaService = inject(MediaService),
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
        this.loadStoreInfo(product.storeId);
        this.loadProductGallery(product.id);
      } catch (err: unknown) {
        patchState(store, {
          error: extractHttpError(err, 'Failed to load product details'),
        });
      } finally {
        patchState(store, { loading: false });
      }
    },

    async loadProductGallery(productId: string): Promise<void> {
      patchState(store, { galleryLoading: true });
      try {
        const gallery = await mediaService.getGallery(productId, 'Product');
        patchState(store, { productGallery: gallery });
      } catch {
        patchState(store, { productGallery: [] });
      } finally {
        patchState(store, { galleryLoading: false });
      }
    },

    async loadSkuGallery(skuId: string): Promise<void> {
      patchState(store, { galleryLoading: true });
      try {
        const gallery = await mediaService.getGallery(skuId, 'SKU');
        patchState(store, { skuGallery: gallery });
      } catch {
        patchState(store, { skuGallery: [] });
      } finally {
        patchState(store, { galleryLoading: false });
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

        // Auto-select the first available combination
        if (matrix && matrix.axes.length > 0) {
          const firstAvailable = matrix.options.find(o => o.isAvailable);
          if (firstAvailable) {
            patchState(store, { selectedVariants: { ...firstAvailable.combination } });
            // Load stock and gallery for the pre-selected SKU
            if (firstAvailable.skuCode) {
              this.loadStock(firstAvailable.skuCode);
            }
            if (firstAvailable.skuId) {
              this.loadSkuGallery(firstAvailable.skuId);
            }
          }
        }
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
      // Load stock and gallery for the matched SKU if available.
      const sku = store.selectedVariantSku();
      if (sku) {
        this.loadStock(sku.skuCode);
        this.loadSkuGallery(sku.id);
      }
    },
  })),
);
