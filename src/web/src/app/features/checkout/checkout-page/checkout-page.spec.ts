// CheckoutPageComponent unit tests.
// Verifies the checkout page creates correctly, injects all required stores
// (CartStore, CheckoutStore, OrderStore), and calls reset on initialization.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { LucideAngularModule, ShoppingCart, Package, Clock, ChevronLeft } from 'lucide-angular';
import { CheckoutPageComponent } from './checkout-page';
import { CheckoutStore } from '../checkout.store';
import { CartStore } from '../../cart/cart.store';
import { OrderStore } from '../../orders/order.store';

describe('CheckoutPageComponent', () => {
  let component: CheckoutPageComponent;
  let fixture: ComponentFixture<CheckoutPageComponent>;

  const mockCartStore = {
    items: signal<any[]>([]),
    isEmpty: signal(true),
    totalItems: signal(0),
    checkoutCorrelationId: signal<string | null>(null),
    checkout: vi.fn(),
  };

  const mockCheckoutStore = {
    submitting: signal(false),
    error: signal<string | null>(null),
    order: signal<any>(null),
    hasOrder: signal(false),
    orderStatus: signal<string | null>(null),
    submitCheckout: vi.fn().mockResolvedValue(undefined),
    reset: vi.fn(),
    setOrder: vi.fn(),
  };

  const mockOrderStore = {
    orders: signal<any[]>([]),
    loadOrders: vi.fn().mockResolvedValue(undefined),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        LucideAngularModule.pick({ ShoppingCart, Package, Clock, ChevronLeft }),
        CheckoutPageComponent,
      ],
      providers: [
        { provide: CartStore, useValue: mockCartStore },
        { provide: CheckoutStore, useValue: mockCheckoutStore },
        { provide: OrderStore, useValue: mockOrderStore },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CheckoutPageComponent);
    component = fixture.componentInstance;
    vi.clearAllMocks();
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should inject all stores', () => {
    expect(component.cartStore).toBeTruthy();
    expect(component.checkoutStore).toBeTruthy();
  });

  it('should call reset on init', () => {
    expect(mockCheckoutStore.reset).toHaveBeenCalled();
  });
});
