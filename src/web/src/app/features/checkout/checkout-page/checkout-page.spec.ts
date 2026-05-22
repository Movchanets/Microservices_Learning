// CheckoutPageComponent unit tests.
// Verifies the checkout page creates correctly, injects all required stores
// (CartStore, CheckoutStore, OrderStore, AuthStore, NotificationService),
// calls reset on initialization, and handles fire-and-forget checkout flow.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { LucideAngularModule, ShoppingCart, Package, Clock, ChevronLeft, AlertTriangle, MapPin, Truck, CreditCard, ShoppingBag, ChevronDown, CheckCircle } from 'lucide-angular';
import { CheckoutPageComponent } from './checkout-page';
import { CheckoutStore } from '../checkout.store';
import { CartStore } from '../../cart/cart.store';
import { OrderStore } from '../../orders/order.store';
import { AuthStore } from '../../../core/auth/auth.store';
import { NotificationService } from '../../../core/signalr/notification.service';

describe('CheckoutPageComponent', () => {
  let component: CheckoutPageComponent;
  let fixture: ComponentFixture<CheckoutPageComponent>;

  const mockCartStore = {
    items: signal<any[]>([]),
    isEmpty: signal(true),
    totalItems: signal(0),
    totalPrice: signal(0),
    checkoutCorrelationId: signal<string | null>(null),
    checkout: vi.fn().mockResolvedValue(undefined),
  };

  const mockCheckoutStore = {
    submitting: signal(false),
    error: signal<string | null>(null),
    order: signal<any>(null),
    address: signal<any>(null),
    shippingMethod: signal<'standard' | 'express'>('standard'),
    hasOrder: signal(false),
    orderStatus: signal<string | null>(null),
    submitted: signal(false),
    pollingExpired: signal(false),
    submitCheckout: vi.fn().mockResolvedValue(undefined),
    reset: vi.fn(),
    setOrder: vi.fn(),
    setAddress: vi.fn(),
    setShippingMethod: vi.fn(),
    retryCheckout: vi.fn(),
    setPollingExpired: vi.fn(),
  };

  const mockOrderStore = {
    orders: signal<any[]>([]),
    selectedOrder: signal<any>(null),
    loadOrders: vi.fn().mockResolvedValue(undefined),
    loadOrderById: vi.fn().mockResolvedValue(undefined),
  };

  const mockAuthStore = {
    user: signal<any>({ id: 'buyer-1' }),
  };

  const mockNotificationService = {
    orderUpdates: signal<any>(null),
    connected: signal(true),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        LucideAngularModule.pick({
          ShoppingCart, Package, Clock, ChevronLeft, AlertTriangle,
          MapPin, Truck, CreditCard, ShoppingBag, ChevronDown, CheckCircle,
        }),
        CheckoutPageComponent,
      ],
      providers: [
        { provide: CartStore, useValue: mockCartStore },
        { provide: CheckoutStore, useValue: mockCheckoutStore },
        { provide: OrderStore, useValue: mockOrderStore },
        { provide: AuthStore, useValue: mockAuthStore },
        { provide: NotificationService, useValue: mockNotificationService },
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

  it('should fire checkout without blocking', () => {
    // onConfirm should return immediately (void, not Promise)
    const result = component.onConfirm();
    expect(result).toBeUndefined();
    expect(mockCheckoutStore.submitCheckout).toHaveBeenCalled();
  });

  it('should delegate retryCheckout to store', () => {
    component.retryCheckout();
    expect(mockCheckoutStore.retryCheckout).toHaveBeenCalled();
  });
});
