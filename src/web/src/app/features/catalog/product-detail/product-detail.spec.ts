// ProductDetailComponent unit tests for selectedVariantTitle computed signal.
// TDD: tests written before implementation.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { importProvidersFrom, NO_ERRORS_SCHEMA } from '@angular/core';
import { LucideAngularModule, ChevronLeft, Store, Tag } from 'lucide-angular';
import { ProductDetailComponent } from './product-detail';
import { CatalogService } from '../catalog.service';
import { MediaService } from '../../../core/services/media.service';
import { InventoryService } from '../../../core/services/inventory.service';
import { RecentlyViewedService } from '../../../core/services/recently-viewed.service';
import { StoreService } from '../../seller-dashboard/store.service';
import { Product, VariantMatrix } from '../catalog.models';

describe('ProductDetailComponent — selectedVariantTitle', () => {
  let component: ProductDetailComponent;
  let fixture: ComponentFixture<ProductDetailComponent>;

  const mockProduct: Product = {
    id: 'prod-1',
    name: 'Навушники Motorola Moto XT220',
    description: 'A test product',
    brand: 'Motorola',
    categoryId: 'cat-1',
    categoryName: 'Headphones',
    status: 'Active',
    imageUrl: '/img/product.jpg',
    storeId: 'store-1',
    tags: [],
    skus: [
      { id: 'sku-1', skuCode: 'WHITE', price: 999, currency: 'USD', status: 'Active', imageUrl: '/img/white.jpg', typedAttributes: { color: 'White' }, flexibleAttributes: {}, createdAt: '2025-01-01' },
      { id: 'sku-2', skuCode: 'BLACK', price: 999, currency: 'USD', status: 'Active', imageUrl: '/img/black.jpg', typedAttributes: { color: 'Black' }, flexibleAttributes: {}, createdAt: '2025-01-01' },
    ],
    gallery: [],
    createdAt: '2025-01-01',
    updatedAt: null,
  };

  const mockVariantMatrix: VariantMatrix = {
    productId: 'prod-1',
    productName: 'Навушники Motorola Moto XT220',
    axes: [
      { key: 'color', displayName: 'Color', values: ['White', 'Black'] },
    ],
    options: [
      { combination: { color: 'White' }, skuId: 'sku-1', skuCode: 'WHITE', price: 999, currency: 'USD', imageUrl: '/img/white.jpg', isAvailable: true },
      { combination: { color: 'Black' }, skuId: 'sku-2', skuCode: 'BLACK', price: 999, currency: 'USD', imageUrl: '/img/black.jpg', isAvailable: true },
    ],
  };

  const mockCatalogService = {
    getProduct: vi.fn().mockResolvedValue(mockProduct),
    getVariantMatrix: vi.fn().mockResolvedValue(mockVariantMatrix),
  };
  const mockMediaService = { getGallery: vi.fn().mockResolvedValue([]) };
  const mockInventoryService = { checkStock: vi.fn().mockResolvedValue({ availableQuantity: 10 }) };
  const mockRecentlyViewedService = { trackView: vi.fn() };
  const mockStoreService = { getStoreById: vi.fn().mockResolvedValue({ storeName: 'Test Store' }) };
  const mockRouter = { navigate: vi.fn() };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductDetailComponent],
      providers: [
        { provide: CatalogService, useValue: mockCatalogService },
        { provide: MediaService, useValue: mockMediaService },
        { provide: InventoryService, useValue: mockInventoryService },
        { provide: RecentlyViewedService, useValue: mockRecentlyViewedService },
        { provide: StoreService, useValue: mockStoreService },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'prod-1' } } } },
        { provide: Router, useValue: mockRouter },
        importProvidersFrom(LucideAngularModule.pick({ ChevronLeft, Store, Tag })),
      ],
    })
      .overrideComponent(ProductDetailComponent, {
        add: { schemas: [NO_ERRORS_SCHEMA] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(ProductDetailComponent);
    component = fixture.componentInstance;
  });

  // ── Cycle 1: no variant selected → base product name ──

  it('should return base product name when no variant is selected', async () => {
    // Load product only — no variant matrix loaded, so selectedVariants stays {}
    await component['store'].loadProduct('prod-1');
    fixture.detectChanges();

    expect(component['selectedVariantTitle']()).toBe('Навушники Motorola Moto XT220');
  });

  // ── Cycle 2: variant selected → title with breadcrumb ──

  it('should append variant attributes when variant is selected', async () => {
    await component['store'].loadProduct('prod-1');
    await component['store'].loadVariantMatrix('prod-1');
    fixture.detectChanges();

    // Auto-selects first available (White)
    expect(component['selectedVariantTitle']()).toBe('Навушники Motorola Moto XT220 — White');
  });

  // ── Cycle 3: variant changes reactively ──

  it('should update title reactively when variant changes', async () => {
    await component['store'].loadProduct('prod-1');
    await component['store'].loadVariantMatrix('prod-1');
    fixture.detectChanges();

    // Initially White (auto-selected)
    expect(component['selectedVariantTitle']()).toContain('White');

    // Switch to Black
    component['store'].selectVariant('color', 'Black');
    fixture.detectChanges();

    expect(component['selectedVariantTitle']()).toBe('Навушники Motorola Moto XT220 — Black');
  });

  // ── Cycle 4: legacy SKU selector → base name (no variant axes) ──

  it('should show base name for legacy SKU selection (no variant axes)', async () => {
    // Legacy product has no variant matrix → selectedVariants stays {}
    mockCatalogService.getVariantMatrix.mockResolvedValueOnce(null);

    await component['store'].loadProduct('prod-1');
    await component['store'].loadVariantMatrix('prod-1');
    fixture.detectChanges();

    // No variant axes → breadcrumb is null → base name only
    expect(component['selectedVariantTitle']()).toBe('Навушники Motorola Moto XT220');
  });
});
