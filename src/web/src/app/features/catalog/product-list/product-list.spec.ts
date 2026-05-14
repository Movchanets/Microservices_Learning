import { vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductListComponent } from './product-list';
import { CatalogStore } from '../catalog.store';
import { CartStore } from '../../cart/cart.store';
import { CatalogService } from '../catalog.service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { LucideAngularModule, Search, Package, Tag, ShoppingCart, SlidersHorizontal, ChevronRight, ChevronLeft, Filter, X } from 'lucide-angular';
import { importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { signal } from '@angular/core';

describe('ProductListComponent', () => {
  let component: ProductListComponent;
  let fixture: ComponentFixture<ProductListComponent>;
  let mockCatalogStore: any;
  let mockCartStore: any;

  beforeEach(async () => {
    // We mock the signals from the store
    mockCatalogStore = {
      isSearchMode: signal(false),
      searchQuery: signal(''),
      categories: signal([]),
      selectedCategoryId: signal(null),
      priceMin: signal(null),
      priceMax: signal(null),
      loading: signal(false),
      error: signal(null),
      totalCount: signal(0),
      products: signal([]),
      page: signal(1),
      totalPages: signal(1),
      hasPrevious: signal(false),
      hasNext: signal(false),
      facets: signal({}),
      loadCategories: vi.fn(),
      loadProducts: vi.fn(),
      refresh: vi.fn(),
      goToPage: vi.fn(),
      updateSearchQuery: vi.fn(),
      selectCategory: vi.fn(),
      setPriceRange: vi.fn(),
    };

    mockCartStore = {
      addToCart: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [ProductListComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        importProvidersFrom(LucideAngularModule.pick({ Search, Package, Tag, ShoppingCart, SlidersHorizontal, ChevronRight, ChevronLeft, Filter, X })),
        CatalogService,
      ],
    })
    .overrideComponent(ProductListComponent, {
      set: {
        providers: [
          { provide: CatalogStore, useValue: mockCatalogStore },
          { provide: CartStore, useValue: mockCartStore },
        ]
      }
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProductListComponent);
    component = fixture.componentInstance;

    // Replace injected store references with our mocks for easy access in tests
    component.store = mockCatalogStore as any;
    component.cartStore = mockCartStore as any;

    fixture.detectChanges();
  });

  it('renders product cards based on store state', () => {
    const products = [
      { id: '1', name: 'Product 1', sku: 'SKU1', currency: 'USD', price: 10, categoryName: 'Cat1' },
      { id: '2', name: 'Product 2', sku: 'SKU2', currency: 'USD', price: 20, categoryName: 'Cat1' }
    ];

    mockCatalogStore.products.set(products);
    mockCatalogStore.totalCount.set(2);
    fixture.detectChanges();

    const productCards = fixture.debugElement.queryAll(By.css('app-product-card'));
    expect(productCards.length).toBe(2);
  });

  it('pagination triggers goToPage in the store', () => {
    mockCatalogStore.totalPages.set(5);
    mockCatalogStore.page.set(2);
    mockCatalogStore.products.set([
      { id: '1', name: 'Product 1', sku: 'SKU1', currency: 'USD', price: 10, categoryName: 'Cat1' }
    ]);
    mockCatalogStore.totalCount.set(100);
    fixture.detectChanges();

    // Trigger page change directly on component
    component.onPageChange(3);

    expect(mockCatalogStore.goToPage).toHaveBeenCalledWith(3);
    expect(mockCatalogStore.refresh).toHaveBeenCalled();
  });
});
