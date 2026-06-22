import { ComponentFixture, TestBed } from '@angular/core/testing';
import { importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { LucideAngularModule, Package, Tag, Store, ShoppingCart } from 'lucide-angular';
import { ProductCardComponent } from './product-card';
import { ProductListItem } from '../../catalog.models';

/** Minimal mock that satisfies ProductListItem */
const mockProduct: ProductListItem = {
  id: 'prod-1',
  name: 'Наушники Hator Hyperpunk 3X 2024 Edition',
  minPrice: 1599,
  maxPrice: 2199,
  currency: 'UAH',
  skuCount: 3,
  defaultSkuId: 'sku-1',
  defaultSkuCode: 'HTR-HP3X',
  categoryName: 'Headphones',
  status: 'Active',
  imageUrl: 'https://example.com/img.jpg',
  storeId: 'store-1',
  createdAt: '2025-01-01T00:00:00Z',
};

describe('ProductCardComponent', () => {
  let component: ProductCardComponent;
  let fixture: ComponentFixture<ProductCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductCardComponent],
      providers: [
        provideRouter([]),
        importProvidersFrom(
          LucideAngularModule.pick({ Package, Tag, Store, ShoppingCart }),
        ),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductCardComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('product', mockProduct);
  });

  // ── Acceptance Criterion 1: Price format ──────────────────────────
  it('should display price as ₴ 1,599.00 with symbol and space', () => {
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    const priceText = el.querySelector('.text-2xl.font-bold')?.textContent?.trim();

    expect(priceText).toBe('₴ 1,599.00');
  });

  it('should fall back to "Price unavailable" when minPrice is null', () => {
    fixture.componentRef.setInput('product', { ...mockProduct, minPrice: null });
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    const text = el.textContent ?? '';

    expect(text).toContain('Price unavailable');
  });

  // ── Acceptance Criterion 2: Title line-clamp-3 ────────────────────
  it('should apply line-clamp-3 to product title', () => {
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    const titleEl = el.querySelector('h3');

    expect(titleEl?.classList.contains('line-clamp-3')).toBe(true);
  });

  // ── Acceptance Criterion 3: Meta text readability ─────────────────
  it('should use text-sm and text-muted-foreground for meta text', () => {
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    const metaEl = el.querySelector('p.text-sm.text-muted-foreground');

    expect(metaEl).toBeTruthy();
    expect(metaEl?.textContent).toContain('variants');
    expect(metaEl?.textContent).toContain('Store');
  });
});
