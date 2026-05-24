// OrderDetailComponent unit tests.
// Tests order detail display, cancel flow, loading state, and error state.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter, ActivatedRoute, convertToParamMap } from '@angular/router';
import { LucideAngularModule, Package, ChevronLeft, CheckCircle2, XCircle, Loader, Check, X } from 'lucide-angular';
import { OrderDetailComponent } from './order-detail';
import { OrderStore } from '../order.store';
import { ToastService } from '../../../core/services/toast.service';

describe('OrderDetailComponent', () => {
  let component: OrderDetailComponent;
  let fixture: ComponentFixture<OrderDetailComponent>;
  let mockStore: any;
  let mockToast: any;

  beforeEach(async () => {
    mockStore = {
      selectedOrder: signal<any>(null),
      loading: signal(false),
      error: signal<string | null>(null),
      loadOrderById: vi.fn().mockResolvedValue(undefined),
      cancelOrder: vi.fn().mockResolvedValue(true),
    };

    mockToast = {
      success: vi.fn(),
      error: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [
        LucideAngularModule.pick({ Package, ChevronLeft, CheckCircle2, XCircle, Loader, Check, X }),
        OrderDetailComponent,
      ],
      providers: [
        { provide: OrderStore, useValue: mockStore },
        { provide: ToastService, useValue: mockToast },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'order-1' }) } },
        },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should inject OrderStore', () => {
    expect(component.store).toBeTruthy();
  });

  it('should show loading spinner when loading', () => {
    mockStore.loading.set(true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.animate-spin')).toBeTruthy();
  });

  it('should show error message when error exists', () => {
    mockStore.loading.set(false);
    mockStore.selectedOrder.set(null);
    mockStore.error.set('Failed to load order details');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Failed to load order details');
  });

  it('should show order details when order is loaded', () => {
    mockStore.selectedOrder.set({
      id: 'order-1',
      buyerId: 'buyer-1',
      status: 'Completed',
      totalAmount: 50,
      createdAt: '2026-01-01T00:00:00Z',
      completedAt: '2026-01-02T00:00:00Z',
      items: [
        { id: 'item-1', sku: 'SKU-1', productName: 'Widget', unitPrice: 25, quantity: 2, totalPrice: 50 },
      ],
    });
    mockStore.loading.set(false);
    mockStore.error.set(null);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Order Details');
    expect(compiled.textContent).toContain('$50');
    expect(compiled.textContent).toContain('Widget');
  });

  it('should show cancel button for cancellable statuses', () => {
    mockStore.selectedOrder.set({
      id: 'order-1',
      buyerId: 'buyer-1',
      status: 'Submitted',
      totalAmount: 50,
      createdAt: '2026-01-01T00:00:00Z',
      completedAt: null,
      items: [],
    });
    mockStore.loading.set(false);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const cancelBtns = Array.from(compiled.querySelectorAll('button')).filter(
      b => b.textContent?.includes('Cancel Order')
    );
    expect(cancelBtns.length).toBeGreaterThan(0);
  });

  it('should not show cancel button for completed orders', () => {
    mockStore.selectedOrder.set({
      id: 'order-1',
      buyerId: 'buyer-1',
      status: 'Completed',
      totalAmount: 50,
      createdAt: '2026-01-01T00:00:00Z',
      completedAt: '2026-01-02T00:00:00Z',
      items: [],
    });
    mockStore.loading.set(false);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const cancelBtns = Array.from(compiled.querySelectorAll('button')).filter(
      b => b.textContent?.includes('Cancel Order')
    );
    expect(cancelBtns.length).toBe(0);
  });

  describe('canCancel', () => {
    it('should return true for Submitted', () => {
      expect(component.canCancel('Submitted')).toBe(true);
    });

    it('should return true for Processing', () => {
      expect(component.canCancel('Processing')).toBe(true);
    });

    it('should return true for PaymentProcessing', () => {
      expect(component.canCancel('PaymentProcessing')).toBe(true);
    });

    it('should return true for InventoryReserved', () => {
      expect(component.canCancel('InventoryReserved')).toBe(true);
    });

    it('should return false for Completed', () => {
      expect(component.canCancel('Completed')).toBe(false);
    });

    it('should return false for Cancelled', () => {
      expect(component.canCancel('Cancelled')).toBe(false);
    });

    it('should return false for Delivered', () => {
      expect(component.canCancel('Delivered')).toBe(false);
    });
  });

  describe('confirmCancel', () => {
    it('should call store.cancelOrder and show success toast', async () => {
      await component.confirmCancel('order-1');

      expect(mockStore.cancelOrder).toHaveBeenCalledWith('order-1', undefined);
      expect(mockToast.success).toHaveBeenCalledWith('Order cancelled successfully');
    });

    it('should pass reason when provided', async () => {
      component.cancelReason = 'changed mind';
      await component.confirmCancel('order-1');

      expect(mockStore.cancelOrder).toHaveBeenCalledWith('order-1', 'changed mind');
    });

    it('should show error toast on failure', async () => {
      mockStore.cancelOrder.mockResolvedValueOnce(false);

      await component.confirmCancel('order-1');

      expect(mockToast.error).toHaveBeenCalledWith('Failed to cancel order');
    });

    it('should reset cancel form after success', async () => {
      component.cancelReason = 'reason';
      component.showCancelConfirm.set(true);

      await component.confirmCancel('order-1');

      expect(component.showCancelConfirm()).toBe(false);
      expect(component.cancelReason).toBe('');
    });

    it('should set cancelling signal during operation', async () => {
      let resolveCancel!: (value: boolean) => void;
      mockStore.cancelOrder.mockReturnValueOnce(new Promise(r => { resolveCancel = r; }));

      const cancelPromise = component.confirmCancel('order-1');
      expect(component.cancelling()).toBe(true);

      resolveCancel(true);
      await cancelPromise;
      expect(component.cancelling()).toBe(false);
    });
  });
});
