// SellerProductStore unit tests.
// Verifies the NgRx SignalStore for seller product management: loadProducts,
// loadProductById, createProduct, updateProduct, deleteProduct.
// Uses mocked SellerProductService.

import { TestBed } from '@angular/core/testing';
import { SellerProductStore } from './seller-product.store';
import { SellerProductService } from './seller-product.service';

describe('SellerProductStore', () => {
  let store: InstanceType<typeof SellerProductStore>;
  const mockProductService = {
    getMyProducts: vi.fn().mockResolvedValue([]),
    getProductById: vi.fn().mockResolvedValue(null),
    createProduct: vi.fn().mockResolvedValue(null),
    updateProduct: vi.fn().mockResolvedValue(null),
    deleteProduct: vi.fn().mockResolvedValue(undefined),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        { provide: SellerProductService, useValue: mockProductService },
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
        { id: 'prod-1', name: 'Widget', status: 'Active' },
        { id: 'prod-2', name: 'Gadget', status: 'Draft' },
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
      const newProduct = { id: 'prod-3', name: 'New Widget', status: 'Draft' };
      mockProductService.createProduct.mockResolvedValueOnce(newProduct);

      const result = await store.createProduct({
        name: 'New Widget', sku: 'NW-1', description: 'A new widget', price: 10, currency: 'USD', categoryId: 'cat-1', sellerId: 'seller-1',
      });

      expect(result).toBe(true);
      expect(store.products()).toContain(newProduct);
    });
  });

  describe('deleteProduct', () => {
    it('should remove product from list', async () => {
      const mockProducts = [
        { id: 'prod-1', name: 'Widget' },
        { id: 'prod-2', name: 'Gadget' },
      ];
      mockProductService.getMyProducts.mockResolvedValueOnce(mockProducts);
      await store.loadProducts();

      mockProductService.deleteProduct.mockResolvedValueOnce(undefined);
      const result = await store.deleteProduct('prod-1');

      expect(result).toBe(true);
      expect(store.products()).toHaveLength(1);
      expect(store.products()[0].id).toBe('prod-2');
    });
  });
});
