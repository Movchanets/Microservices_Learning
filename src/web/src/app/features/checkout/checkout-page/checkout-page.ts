import { Component, ChangeDetectionStrategy, inject, OnInit, OnDestroy, signal, effect, computed, DestroyRef, untracked } from '@angular/core';
import { DecimalPipe, TitleCasePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule, ChevronDown, CheckCircle, CreditCard, Truck, MapPin, ShoppingBag } from 'lucide-angular';
import { CartStore } from '../../cart/cart.store';
import { CheckoutStore } from '../checkout.store';
import { NotificationService } from '../../../core/signalr/notification.service';
import { OrderService } from '../../orders/order.service';
import { AuthStore } from '../../../core/auth/auth.store';
import { CheckoutSummaryComponent } from '../checkout-summary/checkout-summary';
import { CheckoutStatusComponent } from '../checkout-status/checkout-status';
import { AddressFormComponent, Address } from '../address-form/address-form';
import { OrderStatus } from '../checkout.models';

/** Statuses that indicate the order is still processing. */
const ACTIVE_STATUSES: OrderStatus[] = ['Submitted', 'InventoryReserved', 'PaymentProcessing', 'Processing'];

/** Statuses that indicate the order has failed and cannot recover. */
const TERMINAL_FAILURE_STATUSES: OrderStatus[] = ['Cancelled', 'Faulted'];

@Component({
  selector: 'app-checkout-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, TitleCasePipe, RouterLink, LucideAngularModule, CheckoutSummaryComponent, CheckoutStatusComponent, AddressFormComponent],
  templateUrl: './checkout-page.html',
  styleUrl: './checkout-page.css',
})
export class CheckoutPageComponent implements OnInit, OnDestroy {
  readonly cartStore = inject(CartStore);
  readonly checkoutStore = inject(CheckoutStore);
  private readonly notifications = inject(NotificationService);
  private readonly orderService = inject(OrderService);
  private readonly authStore = inject(AuthStore);
  private readonly destroyRef = inject(DestroyRef);

  private pollTimer: ReturnType<typeof setTimeout> | null = null;
  private destroyed = false;
  private submittingCheckout = false;

  // Accordion state
  activeSection = signal<'address' | 'shipping' | 'summary' | 'payment'>('address');

  readonly ChevronIcon = ChevronDown;
  readonly CheckIcon = CheckCircle;
  readonly CardIcon = CreditCard;
  readonly TruckIcon = Truck;
  readonly MapPinIcon = MapPin;
  readonly BagIcon = ShoppingBag;

  constructor() {
    // Clean up on destroy
    this.destroyRef.onDestroy(() => {
      this.destroyed = true;
      this.stopPolling();
    });

    // React to SignalR order status updates for instant UI transitions.
    this.handleSignalRUpdate();
  }

  /**
   * Handles SignalR order updates using a computed signal bridge.
   * Uses `untracked()` for store reads so only the orderUpdates signal
   * triggers re-evaluation — prevents loops from setOrder() re-firing the effect.
   */
  private handleSignalRUpdate(): void {
    // Step 1: Create a computed that combines both trigger signals.
    // Only orderUpdates + submitted should trigger the effect — NOT store.order().
    const trigger = computed(() => {
      const update = this.notifications.orderUpdates();
      const submitted = this.checkoutStore.submitted();
      return update && submitted ? update : null;
    });

    // Step 2: Effect on the single computed signal.
    // All store reads inside use untracked() to avoid the effect re-firing
    // when setOrder()/markTerminalFailure() change the store.
    effect(() => {
      const update = trigger();
      if (!update) return;

      console.log('[Checkout] SignalR effect triggered', {
        orderId: update.orderId,
        status: update.status,
        reason: update.reason,
      });

      // Use untracked() so store reads don't become effect dependencies.
      // Without this, setOrder() inside the effect would re-trigger it.
      untracked(() => {
        const hasOrder = this.checkoutStore.hasOrder();
        const currentOrder = this.checkoutStore.order();

        if (!hasOrder) {
          // No optimistic order yet — set the full order from SignalR.
          console.log('[Checkout] → first update (no optimistic order), setting from SignalR', {
            status: update.status,
          });
          this.checkoutStore.setOrder({
            id: update.orderId,
            buyerId: update.buyerId,
            status: update.status,
            totalAmount: 0,
            createdAt: update.timestamp,
            completedAt: null,
            items: [],
          });
        } else {
          // Optimistic order exists — check if the orderId from SignalR
          // differs from the correlationId we used as placeholder.
          const idMatches = currentOrder && currentOrder.id === update.orderId;
          const statusChanged = currentOrder && currentOrder.status !== update.status;

          console.log('[Checkout] → subsequent update', {
            currentId: currentOrder?.id,
            signalrId: update.orderId,
            idMatches,
            currentStatus: currentOrder?.status,
            newStatus: update.status,
            statusChanged,
          });

          if (!idMatches && currentOrder) {
            // The backend's real orderId differs from our correlationId placeholder.
            // Replace the order with the real ID and update status.
            console.log('[Checkout] → replacing correlationId with real orderId', {
              correlationId: currentOrder.id,
              realOrderId: update.orderId,
            });
            this.checkoutStore.setOrder({
              ...currentOrder,
              id: update.orderId,
              buyerId: update.buyerId || currentOrder.buyerId,
              status: update.status,
            });
            // Restart polling with the real order ID so the fallback works
            this.restartPollingWithId(update.orderId);
          } else if (statusChanged && currentOrder) {
            this.checkoutStore.setOrder({ ...currentOrder, status: update.status });
          }
        }

        // Stop polling on terminal statuses
        if (!ACTIVE_STATUSES.includes(update.status)) {
          console.log('[Checkout] → non-active status, stopping polling', { status: update.status });
          this.stopPolling();
        }

        if (TERMINAL_FAILURE_STATUSES.includes(update.status)) {
          console.log('[Checkout] → terminal failure', { reason: update.reason });
          this.checkoutStore.markTerminalFailure(update.reason);
        }
      });
    });
  }

  ngOnInit(): void {
    const hasOrder = this.checkoutStore.hasOrder();
    const submitted = this.checkoutStore.submitted();
    const error = this.checkoutStore.error();
    const order = this.checkoutStore.order();
    const signalrConnected = this.notifications.connected();
    const signalrUpdate = this.notifications.orderUpdates();

    const orderStatus = order?.status;
    const isTerminal = orderStatus && !ACTIVE_STATUSES.includes(orderStatus);

    console.log('[Checkout] ngOnInit', {
      hasOrder, submitted, error,
      orderStatus: orderStatus ?? null,
      isTerminal,
      signalrConnected,
      signalrUpdate: signalrUpdate ? { orderId: signalrUpdate.orderId, status: signalrUpdate.status } : null,
    });

    if ((!hasOrder && !submitted) || isTerminal) {
      console.log('[Checkout] ngOnInit → resetting store (no active checkout or order is terminal)', { isTerminal });
      this.checkoutStore.reset();
      this.notifications.clearOrderUpdates();
    } else {
      console.log('[Checkout] ngOnInit → preserving state (active checkout detected)');
    }
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  setSection(section: 'address' | 'shipping' | 'summary' | 'payment') {
    this.activeSection.set(section);
  }

  onAddressSaved(address: Address) {
    this.checkoutStore.setAddress(address);
    this.activeSection.set('shipping');
  }

  setShippingMethod(method: 'standard' | 'express') {
    this.checkoutStore.setShippingMethod(method);
    this.activeSection.set('summary');
  }

  async onConfirm(): Promise<void> {
    if (this.submittingCheckout) {
      console.log('[Checkout] onConfirm → blocked (already submitting)');
      return;
    }
    this.submittingCheckout = true;
    console.log('[Checkout] onConfirm → starting checkout');

    try {
      this.notifications.clearOrderUpdates();

      // Ensure SignalR is connected before submitting — if the WebSocket
      // dropped (e.g. network hiccup), reconnect so live status updates arrive.
      if (!this.notifications.connected()) {
        const buyerId = this.authStore.user()?.id;
        console.warn('[Checkout] onConfirm → SignalR not connected, reconnecting', { buyerId });
        if (buyerId) {
          // Fire-and-forget: don't block checkout on SignalR reconnect.
          // Polling acts as fallback if WebSocket reconnection is slow.
          this.notifications.start(buyerId).catch((err) =>
            console.error('[Checkout] onConfirm → SignalR reconnect failed', err)
          );
        }
      } else {
        console.log('[Checkout] onConfirm → SignalR already connected');
      }

      await this.checkoutStore.submitCheckout();

      const error = this.checkoutStore.error();
      const submitted = this.checkoutStore.submitted();
      const correlationId = this.checkoutStore.order()?.id ?? this.cartStore.checkoutCorrelationId();

      console.log('[Checkout] onConfirm → after submitCheckout', {
        error,
        submitted,
        correlationId,
        signalrConnected: this.notifications.connected(),
      });

      if (error || !submitted) {
        console.warn('[Checkout] onConfirm → aborting', { error, submitted });
        return;
      }

      if (correlationId) {
        console.log('[Checkout] onConfirm → starting polling fallback', { correlationId });
        this.startStatusPolling(correlationId);
      } else {
        console.warn('[Checkout] onConfirm → NO correlationId! Polling cannot start. Relying on SignalR only.');
      }
    } finally {
      this.submittingCheckout = false;
    }
  }

  retryCheckout(): void {
    this.stopPolling();
    this.checkoutStore.retryCheckout();
  }

  /**
   * Polls the BFF for a SPECIFIC order by ID every 2s until the order
   * reaches a terminal state (Completed, Cancelled, Faulted) or
   * the max attempts are exhausted.
   */
  private startStatusPolling(orderId: string): void {
    this.stopPolling(); // Prevent double-polling
    console.log('[Checkout] startStatusPolling', { orderId });

    const maxAttempts = 30; // 30 × 2s = 60s max
    let attempt = 0;

    const poll = async () => {
      if (this.destroyed) {
        console.log('[Checkout] poll → destroyed, stopping');
        return;
      }

      attempt++;

      // Stop if the order already reached a terminal state via SignalR
      const currentStatus = this.checkoutStore.orderStatus();
      if (currentStatus && !ACTIVE_STATUSES.includes(currentStatus)) {
        console.log('[Checkout] poll → order already terminal via SignalR', { currentStatus, attempt });
        this.stopPolling();
        return;
      }

      try {
        console.log('[Checkout] poll → fetching', { orderId, attempt });
        const order = await this.orderService.getOrderById(orderId);

        if (order) {
          console.log('[Checkout] poll → got order', { attempt, status: order.status, orderId: order.id });
          this.checkoutStore.setOrder(order);

          if (!ACTIVE_STATUSES.includes(order.status)) {
            console.log('[Checkout] poll → terminal status reached', { status: order.status });
            this.stopPolling();
            if (TERMINAL_FAILURE_STATUSES.includes(order.status)) {
              this.checkoutStore.markTerminalFailure(null);
            }
            return;
          }
        } else {
          console.log('[Checkout] poll → order not found (404)', { attempt, orderId });
        }
      } catch (err: unknown) {
        const status = (err as { status?: number })?.status;
        console.error('[Checkout] poll → error', { err, status, attempt });
      }

      if (attempt < maxAttempts) {
        this.pollTimer = setTimeout(poll, 2000);
      } else {
        console.warn('[Checkout] poll → max attempts reached, setting pollingExpired');
        this.checkoutStore.setPollingExpired(true);
        this.stopPolling();
      }
    };

    // First poll after 500ms — saga typically completes in <100ms
    this.pollTimer = setTimeout(poll, 500);
  }

  /**
   * Restarts polling with a new order ID (e.g. when SignalR reveals
   * the real orderId differs from the correlationId placeholder).
   */
  private restartPollingWithId(newOrderId: string): void {
    const currentStatus = this.checkoutStore.orderStatus();
    if (currentStatus && ACTIVE_STATUSES.includes(currentStatus)) {
      console.log('[Checkout] restartPollingWithId', { newOrderId, currentStatus });
      this.startStatusPolling(newOrderId);
    }
  }

  private stopPolling(): void {
    if (this.pollTimer) {
      console.log('[Checkout] stopPolling → clearing timer');
      clearTimeout(this.pollTimer);
      this.pollTimer = null;
    }
  }
}
