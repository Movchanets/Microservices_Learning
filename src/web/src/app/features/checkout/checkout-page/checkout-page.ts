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



      // Use untracked() so store reads don't become effect dependencies.
      // Without this, setOrder() inside the effect would re-trigger it.
      untracked(() => {
        const hasOrder = this.checkoutStore.hasOrder();
        const currentOrder = this.checkoutStore.order();

        if (!hasOrder) {
          // No optimistic order yet — set the full order from SignalR.

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



          if (!idMatches && currentOrder) {
            // The backend's real orderId differs from our correlationId placeholder.
            // Replace the order with the real ID and update status.

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

          this.stopPolling();
        }

        if (TERMINAL_FAILURE_STATUSES.includes(update.status)) {

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



    if ((!hasOrder && !submitted) || isTerminal) {

      this.checkoutStore.reset();
      this.notifications.clearOrderUpdates();
    } else {

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

      return;
    }
    this.submittingCheckout = true;


    try {
      this.notifications.clearOrderUpdates();

      // Ensure SignalR is connected before submitting — if the WebSocket
      // dropped (e.g. network hiccup), reconnect so live status updates arrive.
      if (!this.notifications.connected()) {
        const buyerId = this.authStore.user()?.id;
        if (buyerId) {
          // Fire-and-forget: don't block checkout on SignalR reconnect.
          // Polling acts as fallback if WebSocket reconnection is slow.
          this.notifications.start(buyerId).catch(() => {});
        }
      }

      await this.checkoutStore.submitCheckout();

      const error = this.checkoutStore.error();
      const submitted = this.checkoutStore.submitted();
      const correlationId = this.checkoutStore.order()?.id ?? this.cartStore.checkoutCorrelationId();

      if (error || !submitted) {
        return;
      }

      if (correlationId) {
        this.startStatusPolling(correlationId);
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


    const maxAttempts = 30; // 30 × 2s = 60s max
    let attempt = 0;

    const poll = async () => {
      if (this.destroyed) {

        return;
      }

      attempt++;

      // Stop if the order already reached a terminal state via SignalR
      const currentStatus = this.checkoutStore.orderStatus();
      if (currentStatus && !ACTIVE_STATUSES.includes(currentStatus)) {

        this.stopPolling();
        return;
      }

      try {

        const order = await this.orderService.getOrderById(orderId);

        if (order) {

          this.checkoutStore.setOrder(order);

          if (!ACTIVE_STATUSES.includes(order.status)) {

            this.stopPolling();
            if (TERMINAL_FAILURE_STATUSES.includes(order.status)) {
              this.checkoutStore.markTerminalFailure(null);
            }
            return;
          }
        } else {

        }
      } catch (err: unknown) {
        const status = (err as { status?: number })?.status;

      }

      if (attempt < maxAttempts) {
        this.pollTimer = setTimeout(poll, 2000);
      } else {

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

      this.startStatusPolling(newOrderId);
    }
  }

  private stopPolling(): void {
    if (this.pollTimer) {

      clearTimeout(this.pollTimer);
      this.pollTimer = null;
    }
  }
}
