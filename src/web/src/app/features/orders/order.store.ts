import { computed, effect, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { OrderService } from './order.service';
import { Order, OrderStatus } from '../checkout/checkout.models';
import { NotificationService } from '../../core/signalr/notification.service';

interface OrderState {
  orders: Order[];
  selectedOrder: Order | null;
  loading: boolean;
  error: string | null;
}

const initialState: OrderState = {
  orders: [],
  selectedOrder: null,
  loading: false,
  error: null,
};

export const OrderStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),

  withComputed((store) => ({
    completedOrders: computed(() => store.orders().filter((o) => o.status === 'Completed')),
    activeOrders: computed(() =>
      store.orders().filter(
        (o) =>
          o.status === 'Submitted' ||
          o.status === 'InventoryReserved' ||
          o.status === 'PaymentProcessing',
      ),
    ),
    hasOrders: computed(() => store.orders().length > 0),
  })),

  withMethods((store, orderService = inject(OrderService)) => ({
    async loadOrders(buyerId: string): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const orders = await orderService.getOrdersByBuyer(buyerId);
        patchState(store, { orders, loading: false });
      } catch {
        patchState(store, { error: 'Failed to load orders', loading: false });
      }
    },

    async loadOrderById(orderId: string): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const order = await orderService.getOrderById(orderId);
        patchState(store, { selectedOrder: order, loading: false });
      } catch {
        patchState(store, { error: 'Failed to load order details', loading: false });
      }
    },

    updateOrderStatus(orderId: string, status: OrderStatus): void {
      patchState(store, {
        orders: store.orders().map((o) => (o.id === orderId ? { ...o, status } : o)),
        selectedOrder:
          store.selectedOrder()?.id === orderId
            ? { ...store.selectedOrder()!, status }
            : store.selectedOrder(),
      });
    },

    async cancelOrder(orderId: string, reason?: string): Promise<boolean> {
      try {
        await orderService.cancelOrder(orderId, reason);
        // Update local state
        patchState(store, {
          orders: store.orders().map(o => o.id === orderId ? { ...o, status: 'Cancelled' as OrderStatus } : o),
          selectedOrder: store.selectedOrder()?.id === orderId
            ? { ...store.selectedOrder()!, status: 'Cancelled' as OrderStatus }
            : store.selectedOrder(),
        });
        return true;
      } catch {
        patchState(store, { error: 'Failed to cancel order' });
        return false;
      }
    },

    clearSelected(): void {
      patchState(store, { selectedOrder: null });
    },
  })),
);
