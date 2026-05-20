import { computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { SellerInventoryService, InventoryItemResponse } from './inventory.service';
import { SellerProductService } from './seller-product.service';

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
  withMethods((store, platformId = inject(PLATFORM_ID)) => {
    const inventoryService = inject(SellerInventoryService);
    const productService = inject(SellerProductService);

    return {
      async loadInventory(): Promise<void> {
        patchState(store, { loading: true, error: null });
        try {
          // Get seller's products first
          const storeId = isPlatformBrowser(platformId) ? (localStorage.getItem('storeId') || '') : '';
          const products = await productService.getMyProducts(storeId);
          const skus = products.map(p => p.sku);

          if (skus.length === 0) {
            patchState(store, { items: [], loading: false });
            return;
          }

          // Get inventory for those SKUs
          const inventory = await inventoryService.getInventoryBySkus(skus);

          // Join product data with inventory data
          const displayItems: InventoryDisplayItem[] = products.map(product => {
            const inv = inventory.find(i => i.sku === product.sku);
            const quantity = inv?.availableQuantity ?? 0;
            return {
              sku: product.sku,
              productName: product.name,
              imageUrl: product.imageUrl,
              quantity,
              status: quantity === 0 ? 'out-of-stock' as const
                : quantity <= LOW_STOCK_THRESHOLD ? 'low-stock' as const
                : 'in-stock' as const,
              lastUpdated: product.updatedAt ?? product.createdAt,
            };
          });

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
