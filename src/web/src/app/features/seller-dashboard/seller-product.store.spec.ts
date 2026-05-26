// SellerProductStore unit tests.
// Verifies the NgRx SignalStore for seller product management: loadProducts,
// loadProductById, createProduct, addSku, removeSku, updateProduct, deleteProduct.
// Uses mocked SellerProductService.

import { TestBed } from '@angular/core/testing';
import { SellerProductStore } from './seller-product.store';
import { SellerProductService } from './seller-product.service';
import { StoreSettingsStore } from './store-settings.store';
import { signal } from '@angular/core';

describe('SellerProductStore', () => {
  let store: InstanceType<typeof SellerProductStore>;
  const mockProductService = {
    getMyProducts: vi.fn().mockResolvedValue([]),
    getProductById: vi.fn().mockResolvedValue(null),
    createProduct: vi.fn().mockResolvedValue(null),
    addSku: vi.fn().mockResolvedValue(null),
    removeSku: vi.fn().mockResolvedValue(undefined),
    updateProduct: vi.fn().mockResolvedValue(null),
    deleteProduct: vi.fn().mockResolvedValue(undefined),
    activateProduct: vi.fn().mockResolvedValue(undefined),
    deactivateProduct: vi.fn().mockResolvedValue(undefined),
  };
  const mockStoreSettingsStore = {
    storeId: signal('store-1'),
    settings: signal({ storeId: 'store-1' }),
    hasSettings: signal(true),
    loading: signal(false),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        { provide: SellerProductService, useValue: mockProductService },
        { provide: StoreSettingsStore, useValue: mockStoreSettingsStore },
      ],
    });

    store = TestBed.inject(SellerProductStore);
    vi.clearAllMocks();
  });

  it('should be created', () => {
    expect(store).toBeTruthy();
  });

  it('should have initial state', () => {
    expect(store.products()).toEqual([]);
    expect(store.selectedProduct()).toBeNull();
    expect(store.loading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  describe('loadProducts', () => {
    it('should load products', async () => {
      const mockProducts = [
        { id: 'prod-1', name: 'Widget', status: 'Active', skus: [] },
        { id: 'prod-2', name: 'Gadget', status: 'Draft', skus: [] },
      ];
      mockProductService.getMyProducts.mockResolvedValueOnce(mockProducts);

      await store.loadProducts();

      expect(store.products()).toEqual(mockProducts);
      expect(store.loading()).toBe(false);
    });

    it('should set loading during fetch', async () => {
      let resolve!: (value?: unknown) => void;
      mockProductService.getMyProducts.mockReturnValueOnce(
        new Promise((r) => { resolve = r; })
      );

      const promise = store.loadProducts();
      expect(store.loading()).toBe(true);

      resolve([]);
      await promise;
      expect(store.loading()).toBe(false);
    });

    it('should handle error', async () => {
      mockProductService.getMyProducts.mockRejectedValueOnce(new Error('fail'));

      await store.loadProducts();

      expect(store.error()).toBe('Failed to load products');
      expect(store.loading()).toBe(false);
    });
  });

  describe('createProduct', () => {
    it('should add product to list', async () => {
      const newProduct = { id: 'prod-3', name: 'New Widget', status: 'Draft', skus: [] };
      mockProductService.createProduct.mockResolvedValueOnce(newProduct);

      const result = await store.createProduct({
        name: 'New Widget', description: 'A new widget', categoryId: 'cat-1', storeId: 'store-1',
      });

      expect(result).toBeTruthy();
      expect(store.products()).toContain(newProduct);
    });

    it('should return null and set error on failure', async () => {
      mockProductService.createProduct.mockRejectedValueOnce(new Error('fail'));

      const result = await store.createProduct({
        name: 'Fail Widget', description: 'x', categoryId: 'cat-1', storeId: 'store-1',
      });

      expect(result).toBeNull();
      expect(store.error()).toBe('Failed to create product');
    });
  });

  describe('addSku', () => {
    it('should add SKU to product in products list', async () => {
      const existingProduct = { id: 'prod-1', name: 'Widget', status: 'Draft', skus: [] };
      mockProductService.getMyProducts.mockResolvedValueOnce([existingProduct]);
      await store.loadProducts();

      const newSku = { id: 'sku-1', skuCode: 'WIDGET-01', price: 29.99, currency: 'USD', status: 'Active', typedAttributes: {}, flexibleAttributes: {}, createdAt: '2026-01-01' };
      mockProductService.addSku.mockResolvedValueOnce(newSku);

      const result = await store.addSku('prod-1', {
        skuCode: 'WIDGET-01', price: 29.99, currency: 'USD',
      });

      expect(result).toEqual(newSku);
      expect(store.products()[0].skus).toContain(newSku);
    });

    it('should add SKU to selectedProduct when IDs match', async () => {
      const product = { id: 'prod-1', name: 'Widget', status: 'Draft', skus: [] };
      mockProductService.getProductById.mockResolvedValueOnce(product);
      await store.loadProductById('prod-1');

      const newSku = { id: 'sku-1', skuCode: 'W-01', price: 10, currency: 'USD', status: 'Active', typedAttributes: {}, flexibleAttributes: {}, createdAt: '2026-01-01' };
      mockProductService.addSku.mockResolvedValueOnce(newSku);

      await store.addSku('prod-1', { skuCode: 'W-01', price: 10, currency: 'USD' });

      expect(store.selectedProduct()!.skus).toContain(newSku);
    });

    it('should return null on failure', async () => {
      mockProductService.addSku.mockRejectedValueOnce(new Error('fail'));

      const result = await store.addSku('prod-1', { skuCode: 'X', price: 1, currency: 'USD' });

      expect(result).toBeNull();
      expect(store.error()).toBe('Failed to add SKU');
    });
  });

  describe('removeSku', () => {
    it('should remove SKU from product in products list', async () => {
      const sku1 = { id: 'sku-1', skuCode: 'W-01', price: 10, currency: 'USD' };
      const sku2 = { id: 'sku-2', skuCode: 'W-02', price: 20, currency: 'USD' };
      const product = { id: 'prod-1', name: 'Widget', status: 'Draft', skus: [sku1, sku2] };
      mockProductService.getMyProducts.mockResolvedValueOnce([product]);
      await store.loadProducts();

      mockProductService.removeSku.mockResolvedValueOnce(undefined);
      const result = await store.removeSku('prod-1', 'sku-1');

      expect(result).toBe(true);
      expect(store.products()[0].skus).toHaveLength(1);
      expect(store.products()[0].skus[0].id).toBe('sku-2');
    });

    it('should remove SKU from selectedProduct when IDs match', async () => {
      const sku1 = { id: 'sku-1', skuCode: 'W-01', price: 10, currency: 'USD' };
      const product = { id: 'prod-1', name: 'Widget', status: 'Draft', skus: [sku1] };
      mockProductService.getProductById.mockResolvedValueOnce(product);
      await store.loadProductById('prod-1');

      mockProductService.removeSku.mockResolvedValueOnce(undefined);
      await store.removeSku('prod-1', 'sku-1');

      expect(store.selectedProduct()!.skus).toHaveLength(0);
    });

    it('should return false on failure', async () => {
      mockProductService.removeSku.mockRejectedValueOnce(new Error('fail'));

      const result = await store.removeSku('prod-1', 'sku-1');

      expect(result).toBe(false);
      expect(store.error()).toBe('Failed to remove SKU');
    });
  });

  describe('deleteProduct', () => {
    it('should remove product from list', async () => {
      const mockProducts = [
        { id: 'prod-1', name: 'Widget', skus: [] },
        { id: 'prod-2', name: 'Gadget', skus: [] },
      ];
      mockProductService.getMyProducts.mockResolvedValueOnce(mockProducts);
      await store.loadProducts();

      mockProductService.deleteProduct.mockResolvedValueOnce(undefined);
      const result = await store.deleteProduct('prod-1');

      expect(result).toBe(true);
      expect(store.products()).toHaveLength(1);
      expect(store.products()[0].id).toBe('prod-2');
    });

    it('should return false on failure', async () => {
      mockProductService.deleteProduct.mockRejectedValueOnce(new Error('fail'));

      const result = await store.deleteProduct('prod-1');

      expect(result).toBe(false);
      expect(store.error()).toBe('Failed to delete product');
    });
  });

  describe('computed signals', () => {
    it('should compute activeProducts', async () => {
      const products = [
        { id: 'p1', name: 'A', status: 'Active', skus: [] },
        { id: 'p2', name: 'B', status: 'Draft', skus: [] },
        { id: 'p3', name: 'C', status: 'Active', skus: [] },
      ];
      mockProductService.getMyProducts.mockResolvedValueOnce(products);
      await store.loadProducts();

      expect(store.activeProducts()).toHaveLength(2);
      expect(store.inactiveProducts()).toHaveLength(1);
      expect(store.hasProducts()).toBe(true);
    });
  });
});
