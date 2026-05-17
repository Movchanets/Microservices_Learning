import { ComponentFixture, TestBed } from '@angular/core/testing';
import { importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { LucideAngularModule, Plus, Package, Loader, ShoppingCart } from 'lucide-angular';
import { FrequentlyBoughtTogetherComponent } from './frequently-bought-together';
import { CartStore } from '../../../cart/cart.store';
import { CartService } from '../../../cart/cart.service';
import { ProductListItem } from '../../catalog.models';

describe('FrequentlyBoughtTogetherComponent', () => {
  let component: FrequentlyBoughtTogetherComponent;
  let fixture: ComponentFixture<FrequentlyBoughtTogetherComponent>;
  let mockCartService: any;

  const mockProducts: ProductListItem[] = [
    { id: '1', name: 'Camera', price: 499, currency: 'USD', sku: 'CAM-1', categoryName: 'Electronics', status: 'Active', imageUrl: null, createdAt: '2026-01-01' },
    { id: '2', name: 'Memory Card', price: 29, currency: 'USD', sku: 'MEM-1', categoryName: 'Electronics', status: 'Active', imageUrl: null, createdAt: '2026-01-01' },
    { id: '3', name: 'Camera Case', price: 49, currency: 'USD', sku: 'CASE-1', categoryName: 'Electronics', status: 'Active', imageUrl: null, createdAt: '2026-01-01' },
  ];

  beforeEach(async () => {
    mockCartService = {
      getCart: vi.fn().mockResolvedValue({ buyerId: 'test', items: [] }),
      updateCart: vi.fn().mockResolvedValue(undefined),
      deleteCart: vi.fn().mockResolvedValue(undefined),
      checkout: vi.fn().mockResolvedValue({ correlationId: 'test' }),
      addItem: vi.fn().mockResolvedValue({ buyerId: 'test', items: [] }),
      updateItem: vi.fn().mockResolvedValue({ buyerId: 'test', items: [] }),
      removeItem: vi.fn().mockResolvedValue({ buyerId: 'test', items: [] }),
    };

    await TestBed.configureTestingModule({
      imports: [FrequentlyBoughtTogetherComponent],
      providers: [
        provideRouter([]),
        { provide: CartService, useValue: mockCartService },
        importProvidersFrom(LucideAngularModule.pick({ Plus, Package, Loader, ShoppingCart })),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FrequentlyBoughtTogetherComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.componentRef.setInput('products', mockProducts);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should show skeleton when loading', () => {
    fixture.componentRef.setInput('products', []);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    const div = fixture.nativeElement.querySelector('.animate-pulse');
    expect(div).toBeTruthy();
  });

  it('should not render when products is empty and not loading', () => {
    fixture.componentRef.setInput('products', []);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).not.toContain('Frequently Bought Together');
  });

  it('should display product names', () => {
    fixture.componentRef.setInput('products', mockProducts);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Camera');
    expect(text).toContain('Memory Card');
    expect(text).toContain('Camera Case');
  });

  it('should display product prices', () => {
    fixture.componentRef.setInput('products', mockProducts);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('499');
    expect(text).toContain('29');
    expect(text).toContain('49');
  });

  it('should calculate total price correctly', () => {
    fixture.componentRef.setInput('products', mockProducts);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();

    expect(component['totalPrice']()).toBe(577);
  });

  it('should use first product currency for bundle price', () => {
    fixture.componentRef.setInput('products', mockProducts);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();

    expect(component['bundleCurrency']()).toBe('USD');
  });

  it('should show product count in add all button', () => {
    fixture.componentRef.setInput('products', mockProducts);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Add All 3 to Cart');
  });

  it('should call cartStore.addToCart for each product on add all', async () => {
    fixture.componentRef.setInput('products', mockProducts);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();

    await component.addAllToCart();

    expect(mockCartService.addItem).toHaveBeenCalledTimes(3);
    expect(mockCartService.addItem).toHaveBeenCalledWith('CAM-1', 1, undefined);
    expect(mockCartService.addItem).toHaveBeenCalledWith('MEM-1', 1, undefined);
    expect(mockCartService.addItem).toHaveBeenCalledWith('CASE-1', 1, undefined);
  });
});
