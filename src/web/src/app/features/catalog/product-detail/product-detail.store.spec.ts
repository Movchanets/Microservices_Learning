// ProductDetailStore unit tests.
// Tests per-SKU gallery loading, product gallery loading, merged gallery
// computed signal, gallery loading state, and variant breadcrumb text.

import { TestBed } from '@angular/core/testing';
import { ProductDetailStore } from './product-detail.store';
import { CatalogService } from '../catalog.service';
import { MediaService } from '../../../core/services/media.service';
import { InventoryService } from '../../../core/services/inventory.service';
import { RecentlyViewedService } from '../../../core/services/recently-viewed.service';
import { StoreService } from '../../seller-dashboard/store.service';
import { GalleryItem, Product, VariantMatrix } from '../catalog.models';
import { SpecEntry } from './product-detail.store';

describe('ProductDetailStore', () => {
  let mockCatalogService: any;
  let mockMediaService: any;
  let mockInventoryService: any;
  let mockRecentlyViewedService: any;
  let mockStoreService: any;
  let store: any;

  // ── Test fixtures ──

  const productGallery: GalleryItem[] = [
    { id: 'g1', fileName: 'product-1.jpg', contentType: 'image/jpeg', url: '/img/product-1.jpg', thumbnailUrl: '/img/product-1-thumb.jpg', sizeBytes: 1000, type: 'Image', sortOrder: 0, isPrimary: true, createdAt: '2025-01-01' },
    { id: 'g2', fileName: 'product-2.jpg', contentType: 'image/jpeg', url: '/img/product-2.jpg', thumbnailUrl: '/img/product-2-thumb.jpg', sizeBytes: 1000, type: 'Image', sortOrder: 1, isPrimary: false, createdAt: '2025-01-01' },
  ];

  const skuGallery: GalleryItem[] = [
    { id: 's1', fileName: 'sku-red-1.jpg', contentType: 'image/jpeg', url: '/img/sku-red-1.jpg', thumbnailUrl: '/img/sku-red-1-thumb.jpg', sizeBytes: 800, type: 'Image', sortOrder: 0, isPrimary: true, createdAt: '2025-01-02' },
    { id: 's2', fileName: 'sku-red-2.jpg', contentType: 'image/jpeg', url: '/img/sku-red-2.jpg', thumbnailUrl: '/img/sku-red-2-thumb.jpg', sizeBytes: 800, type: 'Image', sortOrder: 1, isPrimary: false, createdAt: '2025-01-02' },
  ];

  const mockProduct: Product = {
    id: 'prod-1',
    name: 'Test Product',
    description: 'A test product',
    brand: 'TestBrand',
    categoryId: 'cat-1',
    categoryName: 'Phones',
    status: 'Active',
    imageUrl: '/img/product-main.jpg',
    storeId: 'store-1',
    tags: ['phone'],
    skus: [
      { id: 'sku-1', skuCode: 'RED-128', price: 999, currency: 'USD', status: 'Active', imageUrl: '/img/red.jpg', typedAttributes: { color: 'Red', storage: '128GB' }, flexibleAttributes: {}, createdAt: '2025-01-01' },
      { id: 'sku-2', skuCode: 'BLUE-256', price: 1099, currency: 'USD', status: 'Active', imageUrl: '/img/blue.jpg', typedAttributes: { color: 'Blue', storage: '256GB' }, flexibleAttributes: {}, createdAt: '2025-01-01' },
    ],
    gallery: productGallery,
    createdAt: '2025-01-01',
    updatedAt: null,
  };

  const mockVariantMatrix: VariantMatrix = {
    productId: 'prod-1',
    productName: 'Test Product',
    axes: [
      { key: 'color', displayName: 'Color', values: ['Red', 'Blue'] },
    ],
    options: [
      { combination: { color: 'Red' }, skuId: 'sku-1', skuCode: 'RED-128', price: 999, currency: 'USD', imageUrl: '/img/red.jpg', isAvailable: true },
      { combination: { color: 'Blue' }, skuId: 'sku-2', skuCode: 'BLUE-256', price: 1099, currency: 'USD', imageUrl: '/img/blue.jpg', isAvailable: true },
    ],
  };

  beforeEach(() => {
    mockCatalogService = {
      getProduct: vi.fn().mockResolvedValue(mockProduct),
      getVariantMatrix: vi.fn().mockResolvedValue(mockVariantMatrix),
    };
    mockMediaService = {
      getGallery: vi.fn().mockResolvedValue(productGallery),
    };
    mockInventoryService = {
      checkStock: vi.fn().mockResolvedValue({ availableQuantity: 10 }),
    };
    mockRecentlyViewedService = {
      trackView: vi.fn(),
    };
    mockStoreService = {
      getStoreById: vi.fn().mockResolvedValue({ storeName: 'Test Store' }),
    };

    TestBed.configureTestingModule({
      providers: [
        ProductDetailStore,
        { provide: CatalogService, useValue: mockCatalogService },
        { provide: MediaService, useValue: mockMediaService },
        { provide: InventoryService, useValue: mockInventoryService },
        { provide: RecentlyViewedService, useValue: mockRecentlyViewedService },
        { provide: StoreService, useValue: mockStoreService },
      ],
    });

    store = TestBed.inject(ProductDetailStore);
  });

  // ── specEntries computed ──

  describe('specEntries', () => {
    it('should return empty array when no product is loaded', () => {
      expect(store.specEntries()).toEqual([]);
    });

    it('should return empty array when variant matrix has no axes', async () => {
      await store.loadProduct('prod-1');
      // variantMatrix not loaded → null → no axes
      expect(store.specEntries()).toEqual([]);
    });

    it('should exclude variant axis keys from typedAttributes', async () => {
      // Product has typedAttributes: { color: 'Red', storage: '128GB' }
      // Matrix axes: ['color'] → only 'storage' should appear
      await store.loadProduct('prod-1');
      await store.loadVariantMatrix('prod-1');

      const entries = store.specEntries() as SpecEntry[];
      const keys = entries.map((e: SpecEntry) => e.key);
      expect(keys).not.toContain('color');
      expect(keys).toContain('storage');
    });

    it('should return non-axis attributes with display names', async () => {
      await store.loadProduct('prod-1');
      await store.loadVariantMatrix('prod-1');

      const entries = store.specEntries() as SpecEntry[];
      const storageEntry = entries.find((e: SpecEntry) => e.key === 'storage');
      expect(storageEntry).toBeDefined();
      expect(storageEntry!.value).toBe('128GB');
      // Display name should be a humanized version of the key
      expect(storageEntry!.displayName).toBe('Storage');
    });

    it('should update when variant changes (different SKU)', async () => {
      await store.loadProduct('prod-1');
      await store.loadVariantMatrix('prod-1');

      // Initial: sku-1 → storage: '128GB'
      const initial = store.specEntries() as SpecEntry[];
      expect(initial.find((e: SpecEntry) => e.key === 'storage')?.value).toBe('128GB');

      // Switch to Blue → sku-2 → storage: '256GB'
      store.selectVariant('color', 'Blue');
      const updated = store.specEntries() as SpecEntry[];
      expect(updated.find((e: SpecEntry) => e.key === 'storage')?.value).toBe('256GB');
    });

    it('should return empty array when selected SKU has no typedAttributes', async () => {
      // Product with SKU that has empty typedAttributes
      const productNoAttrs: Product = {
        ...mockProduct,
        skus: [
          { ...mockProduct.skus[0], typedAttributes: {} },
        ],
      };
      mockCatalogService.getProduct.mockResolvedValueOnce(productNoAttrs);

      await store.loadProduct('prod-1');
      await store.loadVariantMatrix('prod-1');

      expect(store.specEntries()).toEqual([]);
    });
  });

  // ── Initial state ──

  it('should initialize with default state', () => {
    expect(store.product()).toBeNull();
    expect(store.loading()).toBe(false);
    expect(store.error()).toBeNull();
    expect(store.productGallery()).toEqual([]);
    expect(store.skuGallery()).toEqual([]);
    expect(store.galleryLoading()).toBe(false);
  });

  // ── loadProductGallery ──

  describe('loadProductGallery', () => {
    it('should fetch product-level gallery from MediaService', async () => {
      await store.loadProductGallery('prod-1');

      expect(mockMediaService.getGallery).toHaveBeenCalledWith('prod-1', 'Product');
      expect(store.productGallery()).toEqual(productGallery);
    });

    it('should set galleryLoading during fetch', async () => {
      let resolve!: (value: unknown) => void;
      mockMediaService.getGallery.mockReturnValueOnce(
        new Promise((r) => { resolve = r; })
      );

      const promise = store.loadProductGallery('prod-1');
      expect(store.galleryLoading()).toBe(true);

      resolve!(productGallery);
      await promise;

      expect(store.galleryLoading()).toBe(false);
    });

    it('should handle gallery fetch failure gracefully', async () => {
      mockMediaService.getGallery.mockRejectedValueOnce(new Error('network'));

      await store.loadProductGallery('prod-1');

      expect(store.productGallery()).toEqual([]);
      expect(store.galleryLoading()).toBe(false);
    });
  });

  // ── loadSkuGallery ──

  describe('loadSkuGallery', () => {
    it('should fetch per-SKU gallery from MediaService', async () => {
      mockMediaService.getGallery.mockResolvedValueOnce(skuGallery);

      await store.loadSkuGallery('sku-1');

      expect(mockMediaService.getGallery).toHaveBeenCalledWith('sku-1', 'SKU');
      expect(store.skuGallery()).toEqual(skuGallery);
    });

    it('should set galleryLoading during fetch', async () => {
      let resolve!: (value: unknown) => void;
      mockMediaService.getGallery.mockReturnValueOnce(
        new Promise((r) => { resolve = r; })
      );

      const promise = store.loadSkuGallery('sku-1');
      expect(store.galleryLoading()).toBe(true);

      resolve!(skuGallery);
      await promise;

      expect(store.galleryLoading()).toBe(false);
    });

    it('should clear skuGallery on fetch failure', async () => {
      // First load successfully
      mockMediaService.getGallery.mockResolvedValueOnce(skuGallery);
      await store.loadSkuGallery('sku-1');
      expect(store.skuGallery()).toEqual(skuGallery);

      // Then fail
      mockMediaService.getGallery.mockRejectedValueOnce(new Error('network'));
      await store.loadSkuGallery('sku-2');

      expect(store.skuGallery()).toEqual([]);
      expect(store.galleryLoading()).toBe(false);
    });
  });

  // ── mergedGallery computed ──

  describe('mergedGallery', () => {
    it('should return product gallery when no SKU gallery exists', async () => {
      await store.loadProductGallery('prod-1');
      expect(store.mergedGallery()).toEqual(productGallery);
    });

    it('should return SKU gallery when SKU gallery is loaded', async () => {
      mockMediaService.getGallery.mockResolvedValueOnce(skuGallery);
      await store.loadSkuGallery('sku-1');

      expect(store.mergedGallery()).toEqual(skuGallery);
    });

    it('should fall back to product gallery when SKU gallery is empty', async () => {
      await store.loadProductGallery('prod-1');

      // Load empty SKU gallery
      mockMediaService.getGallery.mockResolvedValueOnce([]);
      await store.loadSkuGallery('sku-1');

      expect(store.mergedGallery()).toEqual(productGallery);
    });

    it('should switch gallery when variant changes', async () => {
      // Load product gallery first (fallback)
      await store.loadProductGallery('prod-1');

      // Load Red SKU gallery
      mockMediaService.getGallery.mockResolvedValueOnce(skuGallery);
      await store.loadSkuGallery('sku-1');
      expect(store.mergedGallery()).toEqual(skuGallery);

      // Switch to Blue SKU with no gallery
      mockMediaService.getGallery.mockResolvedValueOnce([]);
      await store.loadSkuGallery('sku-2');

      // Should fall back to product gallery
      expect(store.mergedGallery()).toEqual(productGallery);
    });
  });

  // ── selectVariant triggers gallery load ──

  describe('selectVariant gallery integration', () => {
    beforeEach(async () => {
      // Load product + variant matrix (auto-selects first variant)
      await store.loadProduct('prod-1');
      await store.loadVariantMatrix('prod-1');
      // Reset mock call counts for clarity
      mockMediaService.getGallery.mockClear();
    });

    it('should load SKU gallery when variant is selected', async () => {
      mockMediaService.getGallery.mockResolvedValueOnce(skuGallery);

      store.selectVariant('color', 'Red');

      // Wait for async gallery load
      await vi.waitFor(() => {
        expect(store.skuGallery()).toEqual(skuGallery);
      });
      expect(mockMediaService.getGallery).toHaveBeenCalledWith('sku-1', 'SKU');
    });

    it('should load gallery for different SKU when variant changes', async () => {
      const blueGallery: GalleryItem[] = [
        { id: 'b1', fileName: 'sku-blue-1.jpg', contentType: 'image/jpeg', url: '/img/sku-blue-1.jpg', thumbnailUrl: null, sizeBytes: 800, type: 'Image', sortOrder: 0, isPrimary: true, createdAt: '2025-01-03' },
      ];

      // First select Red
      mockMediaService.getGallery.mockResolvedValueOnce(skuGallery);
      store.selectVariant('color', 'Red');
      await vi.waitFor(() => {
        expect(store.skuGallery()).toEqual(skuGallery);
      });

      // Then select Blue
      mockMediaService.getGallery.mockResolvedValueOnce(blueGallery);
      store.selectVariant('color', 'Blue');
      await vi.waitFor(() => {
        expect(store.skuGallery()).toEqual(blueGallery);
      });
    });
  });
});
