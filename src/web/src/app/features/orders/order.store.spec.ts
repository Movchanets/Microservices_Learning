// OrderStore unit tests.
// Tests order loading (by buyer ID and by order ID), computed signals for
// activeOrders and completedOrders filtering, updateOrderStatus for real-time
// status updates, and clearSelected state management.

import { TestBed } from '@angular/core/testing';
import { OrderStore } from './order.store';
import { OrderService } from './order.service';

describe('OrderStore', () => {
  let mockOrderService: any;
  let store: any;

  const mockOrders = [
    { id: 'order-1', buyerId: 'buyer-1', status: 'Completed', totalAmount: 50, items: [] },
    { id: 'order-2', buyerId: 'buyer-1', status: 'Submitted', totalAmount: 30, items: [] },
    { id: 'order-3', buyerId: 'buyer-1', status: 'Cancelled', totalAmount: 20, items: [] },
  ];

  beforeEach(() => {
    mockOrderService = {
      getOrdersByBuyer: vi.fn().mockResolvedValue(mockOrders),
      getOrderById: vi.fn().mockResolvedValue(mockOrders[0]),
      cancelOrder: vi.fn().mockResolvedValue(undefined),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: OrderService, useValue: mockOrderService },
      ],
    });

    store = TestBed.inject(OrderStore);
  });

  it('should initialize with default state', () => {
    expect(store.orders()).toEqual([]);
    expect(store.selectedOrder()).toBeNull();
    expect(store.loading()).toBe(false);
    expect(store.error()).toBeNull();
    expect(store.hasOrders()).toBe(false);
  });

  describe('loadOrders', () => {
    it('should load orders for a buyer', async () => {
      await store.loadOrders('buyer-1');

      expect(mockOrderService.getOrdersByBuyer).toHaveBeenCalledWith('buyer-1');
      expect(store.orders()).toEqual(mockOrders);
      expect(store.loading()).toBe(false);
      expect(store.hasOrders()).toBe(true);
    });

    it('should set loading during fetch', async () => {
      let resolve!: (value?: unknown) => void;
      mockOrderService.getOrdersByBuyer.mockReturnValueOnce(
        new Promise((r) => { resolve = r; })
      );

      const promise = store.loadOrders('buyer-1');
      expect(store.loading()).toBe(true);

      resolve!();
      await promise;

      expect(store.loading()).toBe(false);
    });

    it('should set error on failure', async () => {
      mockOrderService.getOrdersByBuyer.mockRejectedValueOnce(new Error('fail'));

      await store.loadOrders('buyer-1');

      expect(store.error()).toBe('Failed to load orders');
      expect(store.loading()).toBe(false);
    });
  });

  describe('loadOrderById', () => {
    it('should load a single order', async () => {
      await store.loadOrderById('order-1');

      expect(mockOrderService.getOrderById).toHaveBeenCalledWith('order-1');
      expect(store.selectedOrder()).toEqual(mockOrders[0]);
    });

    it('should set error on failure', async () => {
      mockOrderService.getOrderById.mockRejectedValueOnce(new Error('fail'));

      await store.loadOrderById('order-1');

      expect(store.error()).toBe('Failed to load order details');
    });
  });

  describe('computed signals', () => {
    beforeEach(async () => {
      await store.loadOrders('buyer-1');
    });

    it('should filter active orders', () => {
      expect(store.activeOrders()).toEqual([mockOrders[1]]);
    });

    it('should filter completed orders', () => {
      expect(store.completedOrders()).toEqual([mockOrders[0]]);
    });

    it('should report hasOrders', () => {
      expect(store.hasOrders()).toBe(true);
    });
  });

  describe('updateOrderStatus', () => {
    it('should update status of an order in the list', async () => {
      await store.loadOrders('buyer-1');

      store.updateOrderStatus('order-2', 'Completed');

      const updated = store.orders().find((o: any) => o.id === 'order-2');
      expect(updated.status).toBe('Completed');
    });

    it('should update selectedOrder if it matches', async () => {
      await store.loadOrderById('order-1');

      store.updateOrderStatus('order-1', 'Cancelled');

      expect(store.selectedOrder().status).toBe('Cancelled');
    });
  });

  describe('cancelOrder', () => {
    it('should cancel an order and update local state', async () => {
      await store.loadOrders('buyer-1');
      mockOrderService.cancelOrder.mockResolvedValueOnce(undefined);

      const result = await store.cancelOrder('order-2');

      expect(result).toBe(true);
      expect(mockOrderService.cancelOrder).toHaveBeenCalledWith('order-2', undefined);
      const updated = store.orders().find((o: any) => o.id === 'order-2');
      expect(updated.status).toBe('Cancelled');
    });

    it('should cancel with reason', async () => {
      await store.loadOrders('buyer-1');
      mockOrderService.cancelOrder.mockResolvedValueOnce(undefined);

      await store.cancelOrder('order-2', 'changed mind');

      expect(mockOrderService.cancelOrder).toHaveBeenCalledWith('order-2', 'changed mind');
    });

    it('should update selectedOrder if it matches', async () => {
      await store.loadOrderById('order-1');
      mockOrderService.cancelOrder.mockResolvedValueOnce(undefined);

      await store.cancelOrder('order-1');

      expect(store.selectedOrder().status).toBe('Cancelled');
    });

    it('should return false and set error on failure', async () => {
      await store.loadOrders('buyer-1');
      mockOrderService.cancelOrder.mockRejectedValueOnce(new Error('fail'));

      const result = await store.cancelOrder('order-1');

      expect(result).toBe(false);
      expect(store.error()).toBe('Failed to cancel order');
    });
  });

  describe('clearSelected', () => {
    it('should clear the selected order', async () => {
      await store.loadOrderById('order-1');
      store.clearSelected();

      expect(store.selectedOrder()).toBeNull();
    });
  });
});
