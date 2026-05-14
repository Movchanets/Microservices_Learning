import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { CatalogStore } from './catalog.store';
import { CatalogService } from './catalog.service';

describe('CatalogStore', () => {
  let catalogServiceMock: any;

  beforeEach(() => {
    catalogServiceMock = {
      getProducts: vi.fn(),
      searchProducts: vi.fn(),
      getCategories: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        CatalogStore,
        { provide: CatalogService, useValue: catalogServiceMock },
      ],
    });
  });

  describe('isSearchMode', () => {
    it('returns true when searchQuery is not empty', () => {
      const store = TestBed.inject(CatalogStore);
      expect(store.isSearchMode()).toBe(false);

      store.updateSearchQuery('laptop');
      expect(store.isSearchMode()).toBe(true);

      store.updateSearchQuery('  ');
      expect(store.isSearchMode()).toBe(false);
    });
  });

  describe('totalPages', () => {
    it('calculates totalPages based on totalCount and pageSize', async () => {
      catalogServiceMock.getProducts.mockReturnValue(Promise.resolve({
        items: [],
        totalCount: 45,
      }));

      const store = TestBed.inject(CatalogStore);

      // Initially 0 items, pageSize is 20 -> 0 pages
      expect(store.totalPages()).toBe(0);

      await store.loadProducts();

      // 45 items, pageSize 20 -> 3 pages
      expect(store.totalPages()).toBe(3);
    });
  });

  describe('loadProducts', () => {
    it('correctly updates products and totalCount state', async () => {
      const mockProducts = [
        { id: '1', name: 'Product 1', sku: 'SKU1', currency: 'USD', price: 10, categoryName: 'Cat1' }
      ];
      catalogServiceMock.getProducts.mockReturnValue(Promise.resolve({
        items: mockProducts,
        totalCount: 1,
      }));

      const store = TestBed.inject(CatalogStore);
      expect(store.loading()).toBe(false);

      const promise = store.loadProducts();

      // Before resolving, it should be loading
      expect(store.loading()).toBe(true);

      await promise;

      expect(store.loading()).toBe(false);
      expect(store.products()).toEqual(mockProducts);
      expect(store.totalCount()).toBe(1);
    });
  });

  describe('updateSearchQuery', () => {
    it('resets page to 1 when search query is updated', () => {
      const store = TestBed.inject(CatalogStore);
      store.goToPage(3);
      expect(store.page()).toBe(3);

      store.updateSearchQuery('test');
      expect(store.page()).toBe(1);
      expect(store.searchQuery()).toBe('test');
    });
  });
});
