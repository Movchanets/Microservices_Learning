import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { RouterTestingModule } from '@angular/router/testing';
import { LucideAngularModule, ShoppingCart, Package, Minus, Plus, Trash2, CheckCircle2 } from 'lucide-angular';
import { CartPageComponent } from './cart-page';
import { CartStore } from '../cart.store';

describe('CartPageComponent', () => {
  let component: CartPageComponent;
  let fixture: ComponentFixture<CartPageComponent>;

  const mockItems = signal([]);
  const mockLoading = signal(false);
  const mockError = signal(null);
  const mockIsEmpty = signal(true);
  const mockTotalItems = signal(0);
  const mockCheckoutCorrelationId = signal(null);

  const mockCartStore = {
    items: mockItems,
    loading: mockLoading,
    error: mockError,
    isEmpty: mockIsEmpty,
    totalItems: mockTotalItems,
    checkoutCorrelationId: mockCheckoutCorrelationId,
    updateQuantity: vi.fn(),
    removeFromCart: vi.fn(),
    checkout: vi.fn()
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        RouterTestingModule,
        LucideAngularModule.pick({ ShoppingCart, Package, Minus, Plus, Trash2, CheckCircle2 }),
        CartPageComponent
      ],
      providers: [
        { provide: CartStore, useValue: mockCartStore }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CartPageComponent);
    component = fixture.componentInstance;

    // Reset signals
    mockItems.set([]);
    mockLoading.set(false);
    mockError.set(null);
    mockIsEmpty.set(true);
    mockTotalItems.set(0);
    mockCheckoutCorrelationId.set(null);

    // Reset mocks
    vi.clearAllMocks();

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display empty cart message when empty', () => {
    mockIsEmpty.set(true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Your cart is empty');
  });

  it('should render items with correct quantities', () => {
    mockIsEmpty.set(false);
    mockItems.set([{ sku: 'PROD-1', quantity: 2 }, { sku: 'PROD-2', quantity: 1 }] as any);
    mockTotalItems.set(3);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    // Check for product SKUs
    expect(compiled.textContent).toContain('PROD-1');
    expect(compiled.textContent).toContain('PROD-2');

    // Count the list items
    const listItems = compiled.querySelectorAll('li');
    expect(listItems.length).toBe(2);

    // Check total items display
    const totalItemsElements = Array.from(compiled.querySelectorAll('p')).filter(p => p.textContent?.trim() === '3');
    expect(compiled.textContent).toContain('Total Items');
  });

  it('should call checkout on Checkout button click', () => {
    mockIsEmpty.set(false);
    mockItems.set([{ sku: 'PROD-1', quantity: 2 }] as any);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const checkoutButton = Array.from(compiled.querySelectorAll('button')).find(
      b => b.textContent?.trim() === 'Checkout'
    );

    expect(checkoutButton).toBeTruthy();
    checkoutButton?.click();

    expect(mockCartStore.checkout).toHaveBeenCalled();
  });

  it('should call updateQuantity when clicking plus/minus buttons', () => {
    mockIsEmpty.set(false);
    mockItems.set([{ sku: 'PROD-1', quantity: 2 }] as any);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    // Since lucide icons are rendered inside buttons, find buttons with icon names
    const buttons = compiled.querySelectorAll('button');
    const minusBtn = Array.from(buttons).find(b => b.innerHTML.includes('lucide-icon') && b.innerHTML.includes('Minus') || b.querySelector('[name="Minus"]'));
    const plusBtn = Array.from(buttons).find(b => b.innerHTML.includes('lucide-icon') && b.innerHTML.includes('Plus') || b.querySelector('[name="Plus"]'));
    const trashBtn = Array.from(buttons).find(b => b.innerHTML.includes('lucide-icon') && b.innerHTML.includes('Trash2') || b.querySelector('[name="Trash2"]'));

    // We can also trigger click directly via elements
    if (minusBtn) (minusBtn as HTMLElement).click();
    expect(mockCartStore.updateQuantity).toHaveBeenCalledWith('PROD-1', 1);

    if (plusBtn) (plusBtn as HTMLElement).click();
    expect(mockCartStore.updateQuantity).toHaveBeenCalledWith('PROD-1', 3);

    if (trashBtn) (trashBtn as HTMLElement).click();
    expect(mockCartStore.removeFromCart).toHaveBeenCalledWith('PROD-1');
  });

  it('should display success message when checkoutCorrelationId is set', () => {
    mockIsEmpty.set(false);
    mockItems.set([]);
    mockCheckoutCorrelationId.set('corr-123' as any);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Order Submitted!');
    expect(compiled.textContent).toContain('corr-123');
  });
});
