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
  const mockTotalPrice = signal(0);

  const mockCartStore = {
    items: mockItems, loading: mockLoading, error: signal<string | null>(null),
    isEmpty: mockIsEmpty, totalItems: mockTotalItems, totalPrice: mockTotalPrice,
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
    mockItems.set([]); mockIsEmpty.set(true); mockTotalItems.set(0); mockTotalPrice.set(0);
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
    mockItems.set([
      { productId: 'PROD-1', storeId: 's1', quantity: 2, title: 'Widget', price: 10, lineTotal: 20, imageUrl: null },
      { productId: 'PROD-2', storeId: 's1', quantity: 1, title: 'Gadget', price: 15, lineTotal: 15, imageUrl: null },
    ]);
    mockTotalItems.set(3);
    mockTotalPrice.set(35);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Widget');
  });

  it('should call updateQuantity on minus click', () => {
    mockIsEmpty.set(false);
    mockItems.set([{ productId: 'PROD-1', storeId: 's1', quantity: 2, title: 'Widget', price: 10, lineTotal: 20, imageUrl: null }]);
    mockTotalItems.set(2);
    mockTotalPrice.set(20);
    fixture.detectChanges();
    const btn = fixture.nativeElement.querySelector('[data-testid="cart-item-decrease"]');
    if (btn) btn.click();
    expect(mockCartStore.updateQuantity).toHaveBeenCalledWith('PROD-1', 1);
  });

  it('should call removeFromCart on trash click', () => {
    mockIsEmpty.set(false);
    mockItems.set([{ productId: 'PROD-1', storeId: 's1', quantity: 2, title: 'Widget', price: 10, lineTotal: 20, imageUrl: null }]);
    mockTotalItems.set(2);
    mockTotalPrice.set(20);
    fixture.detectChanges();
    const btn = fixture.nativeElement.querySelector('[data-testid="cart-item-remove"]');
    if (btn) btn.click();
    expect(mockCartStore.removeFromCart).toHaveBeenCalledWith('PROD-1');
  });
});
