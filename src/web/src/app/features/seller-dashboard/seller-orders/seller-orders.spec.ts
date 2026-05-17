// SellerOrdersComponent unit tests.
// Tests order listing, status update flow, getNextStatus logic, and statusClass.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { importProvidersFrom } from '@angular/core';
import { LucideAngularModule, Package, ArrowRight, Loader } from 'lucide-angular';
import { SellerOrdersComponent } from './seller-orders';
import { AuthStore } from '../../../core/auth/auth.store';
import { OrderService } from '../../orders/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';

describe('SellerOrdersComponent', () => {
  let component: SellerOrdersComponent;
  let fixture: ComponentFixture<SellerOrdersComponent>;

  const mockOrders = [
    { id: 'order-1', buyerId: 'buyer-1', status: 'Submitted', totalAmount: 50, createdAt: '2026-01-01T00:00:00Z', items: [] },
    { id: 'order-2', buyerId: 'buyer-2', status: 'Processing', totalAmount: 30, createdAt: '2026-01-02T00:00:00Z', items: [] },
    { id: 'order-3', buyerId: 'buyer-3', status: 'Shipped', totalAmount: 20, createdAt: '2026-01-03T00:00:00Z', items: [] },
    { id: 'order-4', buyerId: 'buyer-4', status: 'Completed', totalAmount: 10, createdAt: '2026-01-04T00:00:00Z', items: [] },
  ];

  const mockHttpClient = {
    get: vi.fn().mockReturnValue(of(mockOrders)),
  };

  const mockAuthStore = {
    user: signal({ id: 'seller-1', firstName: 'Seller', email: 'seller@test.com' }),
  };

  const mockOrderService = {
    updateOrderStatus: vi.fn().mockResolvedValue(undefined),
  };

  const mockToast = {
    success: vi.fn(),
    error: vi.fn(),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        LucideAngularModule.pick({ Package, ArrowRight, Loader }),
        SellerOrdersComponent,
      ],
      providers: [
        { provide: HttpClient, useValue: mockHttpClient },
        { provide: AuthStore, useValue: mockAuthStore },
        { provide: OrderService, useValue: mockOrderService },
        { provide: ToastService, useValue: mockToast },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SellerOrdersComponent);
    component = fixture.componentInstance;
    vi.clearAllMocks();
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load orders on init', () => {
    expect(mockHttpClient.get).toHaveBeenCalledWith('/api/orders/seller/seller-1');
    expect(component.orders()).toEqual(mockOrders);
  });

  it('should show loading state', () => {
    component.loading.set(true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.animate-spin')).toBeTruthy();
  });

  it('should show empty state when no orders', () => {
    component.orders.set([]);
    component.loading.set(false);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No orders yet');
  });

  describe('getNextStatus', () => {
    it('should return Processing for Submitted', () => {
      expect(component.getNextStatus('Submitted')).toBe('Processing');
    });

    it('should return Shipped for Processing', () => {
      expect(component.getNextStatus('Processing')).toBe('Shipped');
    });

    it('should return Delivered for Shipped', () => {
      expect(component.getNextStatus('Shipped')).toBe('Delivered');
    });

    it('should return null for Completed', () => {
      expect(component.getNextStatus('Completed')).toBeNull();
    });

    it('should return null for Cancelled', () => {
      expect(component.getNextStatus('Cancelled')).toBeNull();
    });

    it('should return null for Delivered', () => {
      expect(component.getNextStatus('Delivered')).toBeNull();
    });
  });

  describe('confirmStatusUpdate', () => {
    it('should call orderService.updateOrderStatus', async () => {
      await component.confirmStatusUpdate('order-1', 'Processing');

      expect(mockOrderService.updateOrderStatus).toHaveBeenCalledWith('order-1', 'Processing', undefined);
    });

    it('should pass notes when provided', async () => {
      component.updateNotes = 'Started processing';
      await component.confirmStatusUpdate('order-1', 'Processing');

      expect(mockOrderService.updateOrderStatus).toHaveBeenCalledWith('order-1', 'Processing', 'Started processing');
    });

    it('should update local orders state', async () => {
      await component.confirmStatusUpdate('order-1', 'Processing');

      const updated = component.orders().find(o => o.id === 'order-1');
      expect(updated?.status).toBe('Processing');
    });

    it('should show success toast', async () => {
      await component.confirmStatusUpdate('order-1', 'Processing');

      expect(mockToast.success).toHaveBeenCalledWith('Order marked as Processing');
    });

    it('should show error toast on failure', async () => {
      mockOrderService.updateOrderStatus.mockRejectedValueOnce(new Error('fail'));

      await component.confirmStatusUpdate('order-1', 'Processing');

      expect(mockToast.error).toHaveBeenCalledWith('Failed to update order status');
    });

    it('should clear updatingId after success', async () => {
      component.updatingId.set('order-1');
      await component.confirmStatusUpdate('order-1', 'Processing');

      expect(component.updatingId()).toBeNull();
    });
  });

  describe('statusClass', () => {
    it('should return blue for Submitted', () => {
      expect(component.statusClass('Submitted')).toContain('blue');
    });

    it('should return purple for Processing', () => {
      expect(component.statusClass('Processing')).toContain('purple');
    });

    it('should return indigo for Shipped', () => {
      expect(component.statusClass('Shipped')).toContain('indigo');
    });

    it('should return green for Delivered', () => {
      expect(component.statusClass('Delivered')).toContain('green');
    });

    it('should return green for Completed', () => {
      expect(component.statusClass('Completed')).toContain('green');
    });

    it('should return red for Cancelled', () => {
      expect(component.statusClass('Cancelled')).toContain('red');
    });
  });
});
