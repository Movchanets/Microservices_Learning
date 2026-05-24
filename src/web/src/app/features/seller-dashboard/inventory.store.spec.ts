// InventoryStore unit tests.
// Tests inventory loading, stock status classification, lowStockItems computed,
// and addStock functionality.

import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { InventoryStore } from './inventory.store';
import { SellerInventoryService } from './inventory.service';
import { SellerProductService } from './seller-product.service';
import { StoreSettingsStore } from './store-settings.store';

describe('InventoryStore', () => {
  let store: InstanceType<typeof InventoryStore>;
  let mockInventoryService: any;
  let mockProductService: any;

  const mockProducts = [
    { id: 'p1', name: 'Widget Pro', sku: 'WP-1', imageUrl: null, updatedAt: '2026-01-01', createdAt: '2026-01-01' },
    { id: 'p2', name: 'Gadget Mini', sku: 'GM-1', imageUrl: null, updatedAt: '2026-01-01', createdAt: '2026-01-01' },
    { id: 'p3', name: 'Thing Max', sku: 'TM-1', imageUrl: null, updatedAt: '2026-01-01', createdAt: '2026-01-01' },
  ];

  const mockInventory = [
    { id: 'i1', sku: 'WP-1', availableQuantity: 15 },
    { id: 'i2', sku: 'GM-1', availableQuantity: 3 },
    { id: 'i3', sku: 'TM-1', availableQuantity: 0 },
  ];

  beforeEach(() => {
    mockInventoryService = {
      getInventoryBySkus: vi.fn().mockResolvedValue(mockInventory),
      addStock: vi.fn().mockResolvedValue(undefined),
    };
    mockProductService = {
      getMyProducts: vi.fn().mockResolvedValue(mockProducts),
    };
    const mockStoreSettingsStore = {
      storeId: signal('store-1'),
      settings: signal({ storeId: 'store-1' }),
      hasSettings: signal(true),
      loading: signal(false),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: SellerInventoryService, useValue: mockInventoryService },
        { provide: SellerProductService, useValue: mockProductService },
        { provide: StoreSettingsStore, useValue: mockStoreSettingsStore },
      ],
    });

    store = TestBed.inject(InventoryStore);
    vi.clearAllMocks();
  });

  it('should be created', () => {
    expect(store).toBeTruthy();
  });

  it('should have initial state', () => {
    expect(store.items()).toEqual([]);
    expect(store.loading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  describe('loadInventory', () => {
    it('should load products and inventory', async () => {
      await store.loadInventory();

      expect(mockProductService.getMyProducts).toHaveBeenCalledWith('store-1');
      expect(mockInventoryService.getInventoryBySkus).toHaveBeenCalledWith(['WP-1', 'GM-1', 'TM-1']);
    });

    it('should join product and inventory data', async () => {
      await store.loadInventory();

      const items = store.items();
      expect(items).toHaveLength(3);

      expect(items[0].sku).toBe('WP-1');
      expect(items[0].productName).toBe('Widget Pro');
      expect(items[0].quantity).toBe(15);
      expect(items[0].status).toBe('in-stock');

      expect(items[1].sku).toBe('GM-1');
      expect(items[1].quantity).toBe(3);
      expect(items[1].status).toBe('low-stock');

      expect(items[2].sku).toBe('TM-1');
      expect(items[2].quantity).toBe(0);
      expect(items[2].status).toBe('out-of-stock');
    });

    it('should set loading during fetch', async () => {
      let resolve!: (value?: unknown) => void;
      mockProductService.getMyProducts.mockReturnValueOnce(
        new Promise((r) => { resolve = r; })
      );

      const promise = store.loadInventory();
      expect(store.loading()).toBe(true);

      resolve(mockProducts);
      await promise;
      expect(store.loading()).toBe(false);
    });

    it('should set error on failure', async () => {
      mockProductService.getMyProducts.mockRejectedValueOnce(new Error('fail'));

      await store.loadInventory();

      expect(store.error()).toBe('Failed to load inventory');
      expect(store.loading()).toBe(false);
    });

    it('should handle empty products', async () => {
      mockProductService.getMyProducts.mockResolvedValueOnce([]);

      await store.loadInventory();

      expect(store.items()).toEqual([]);
      expect(mockInventoryService.getInventoryBySkus).not.toHaveBeenCalled();
    });

    it('should default quantity to 0 when inventory not found', async () => {
      mockInventoryService.getInventoryBySkus.mockResolvedValueOnce([]);

      await store.loadInventory();

      expect(store.items()).toHaveLength(3);
      store.items().forEach(item => {
        expect(item.quantity).toBe(0);
        expect(item.status).toBe('out-of-stock');
      });
    });
  });

  describe('computed signals', () => {
    beforeEach(async () => {
      await store.loadInventory();
    });

    it('should compute lowStockItems', () => {
      const lowStock = store.lowStockItems();
      expect(lowStock).toHaveLength(2);
      expect(lowStock.map(i => i.sku)).toContain('GM-1');
      expect(lowStock.map(i => i.sku)).toContain('TM-1');
    });

    it('should compute lowStockCount', () => {
      expect(store.lowStockCount()).toBe(2);
    });
  });

  describe('addStock', () => {
    it('should call inventoryService.addStock and reload', async () => {
      mockInventoryService.addStock.mockResolvedValueOnce(undefined);
      mockProductService.getMyProducts.mockResolvedValueOnce(mockProducts);

      const result = await store.addStock('WP-1', 10);

      expect(result).toBe(true);
      expect(mockInventoryService.addStock).toHaveBeenCalledWith('WP-1', 10);
    });

    it('should return false and set error on failure', async () => {
      mockInventoryService.addStock.mockRejectedValueOnce(new Error('fail'));

      const result = await store.addStock('WP-1', 10);

      expect(result).toBe(false);
      expect(store.error()).toBe('Failed to add stock');
    });
  });
});
