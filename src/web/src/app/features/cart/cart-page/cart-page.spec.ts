import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { LucideAngularModule, ShoppingCart, Package, Minus, Plus, Trash2, CheckCircle2 } from 'lucide-angular';
import { CartPageComponent } from './cart-page';
import { CartStore } from '../cart.store';

describe('CartPageComponent', () => {
  let component: CartPageComponent;
  let fixture: ComponentFixture<CartPageComponent>;

  const mockItems = signal<any[]>([]);
  const mockLoading = signal(false);
  const mockIsEmpty = signal(true);
  const mockTotalItems = signal(0);

  const mockCartStore = {
    items: mockItems, loading: mockLoading, error: signal<string | null>(null),
    isEmpty: mockIsEmpty, totalItems: mockTotalItems,
    checkoutCorrelationId: signal<string | null>(null),
    updateQuantity: vi.fn(), removeFromCart: vi.fn(), checkout: vi.fn(),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LucideAngularModule.pick({ ShoppingCart, Package, Minus, Plus, Trash2, CheckCircle2 }), CartPageComponent],
      providers: [{ provide: CartStore, useValue: mockCartStore }, provideRouter([])],
    }).compileComponents();
    fixture = TestBed.createComponent(CartPageComponent);
    component = fixture.componentInstance;
    mockItems.set([]); mockIsEmpty.set(true); mockTotalItems.set(0);
    vi.clearAllMocks();
    fixture.detectChanges();
  });

  it('should create', () => { expect(component).toBeTruthy(); });

  it('should display empty cart message', () => {
    mockIsEmpty.set(true); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Your cart is empty');
  });

  it('should render items', () => {
    mockIsEmpty.set(false);
    mockItems.set([{ productId: 'PROD-1', storeId: 's1', quantity: 2 }, { productId: 'PROD-2', storeId: 's1', quantity: 1 }]);
    mockTotalItems.set(3); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('PROD-1');
  });

  it('should call updateQuantity on minus click', () => {
    mockIsEmpty.set(false);
    mockItems.set([{ productId: 'PROD-1', storeId: 's1', quantity: 2 }]); fixture.detectChanges();
    const btn = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.querySelector('lucide-icon[name="Minus"]'));
    if (btn) (btn as HTMLElement).click();
    expect(mockCartStore.updateQuantity).toHaveBeenCalledWith('PROD-1', 1);
  });

  it('should call removeFromCart on trash click', () => {
    mockIsEmpty.set(false);
    mockItems.set([{ productId: 'PROD-1', storeId: 's1', quantity: 2 }]); fixture.detectChanges();
    const btn = Array.from(fixture.nativeElement.querySelectorAll('button')).find(
      (b: any) => b.querySelector('lucide-icon[name="Trash2"]'));
    if (btn) (btn as HTMLElement).click();
    expect(mockCartStore.removeFromCart).toHaveBeenCalledWith('PROD-1');
  });
});
