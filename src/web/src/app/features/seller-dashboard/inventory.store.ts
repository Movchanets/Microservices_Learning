import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { SellerInventoryService, InventoryItemResponse } from './inventory.service';
import { SellerProductService } from './seller-product.service';
import { StoreSettingsStore } from './store-settings.store';

export interface InventoryDisplayItem {
  sku: string;
  productName: string;
  imageUrl: string | null;
  quantity: number;
  status: 'in-stock' | 'low-stock' | 'out-of-stock';
  lastUpdated: string;
}

interface InventoryState {
  items: InventoryDisplayItem[];
  loading: boolean;
  error: string | null;
}

const LOW_STOCK_THRESHOLD = 5;

const initialState: InventoryState = {
  items: [],
  loading: false,
  error: null,
};

export const InventoryStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((store) => ({
    lowStockItems: computed(() =>
      store.items().filter(i => i.status === 'low-stock' || i.status === 'out-of-stock'),
    ),
    lowStockCount: computed(() =>
      store.items().filter(i => i.status === 'low-stock' || i.status === 'out-of-stock').length,
    ),
  })),
  withMethods((store, storeSettingsStore = inject(StoreSettingsStore), inventoryService = inject(SellerInventoryService), productService = inject(SellerProductService)) => {

    return {
      async loadInventory(): Promise<void> {
        patchState(store, { loading: true, error: null });
        try {
          const storeId = storeSettingsStore.storeId() ?? '';
          if (!storeId) {
            patchState(store, { items: [], loading: false });
            return;
          }
          const products = await productService.getMyProducts(storeId);

          // Collect all SKU codes from all products
          const allSkuCodes = products.flatMap(p => (p.skus ?? []).map(s => s.skuCode));

          if (allSkuCodes.length === 0) {
            patchState(store, { items: [], loading: false });
            return;
          }

          // Get inventory for those SKUs
          const inventory = await inventoryService.getInventoryBySkus(allSkuCodes);

          // Join product data with inventory data — one display item per SKU
          const displayItems: InventoryDisplayItem[] = [];
          for (const product of products) {
            for (const sku of product.skus ?? []) {
              const inv = inventory.find(i => i.skuCode === sku.skuCode);
              const quantity = inv?.availableQuantity ?? 0;
              displayItems.push({
                sku: sku.skuCode,
                productName: product.name,
                imageUrl: product.imageUrl,
                quantity,
                status: quantity === 0 ? 'out-of-stock' as const
                  : quantity <= LOW_STOCK_THRESHOLD ? 'low-stock' as const
                  : 'in-stock' as const,
                lastUpdated: product.updatedAt ?? product.createdAt,
              });
            }
          }

          patchState(store, { items: displayItems, loading: false });
        } catch {
          patchState(store, { error: 'Failed to load inventory', loading: false });
        }
      },

      async addStock(sku: string, quantity: number): Promise<boolean> {
        try {
          await inventoryService.addStock(sku, quantity);
          // Reload to get updated data
          await this.loadInventory();
          return true;
        } catch {
          patchState(store, { error: 'Failed to add stock' });
          return false;
        }
      },
    };
  }),
);
