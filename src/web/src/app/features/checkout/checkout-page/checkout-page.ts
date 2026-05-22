import { Component, ChangeDetectionStrategy, inject, OnInit, OnDestroy, signal, effect } from '@angular/core';
import { DecimalPipe, TitleCasePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule, ChevronDown, CheckCircle, CreditCard, Truck, MapPin, ShoppingBag } from 'lucide-angular';
import { CartStore } from '../../cart/cart.store';
import { CheckoutStore } from '../checkout.store';
import { OrderStore } from '../../orders/order.store';
import { AuthStore } from '../../../core/auth/auth.store';
import { NotificationService } from '../../../core/signalr/notification.service';
import { CheckoutSummaryComponent } from '../checkout-summary/checkout-summary';
import { CheckoutStatusComponent } from '../checkout-status/checkout-status';
import { AddressFormComponent, Address } from '../address-form/address-form';

@Component({
  selector: 'app-checkout-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, TitleCasePipe, RouterLink, LucideAngularModule, CheckoutSummaryComponent, CheckoutStatusComponent, AddressFormComponent],
  templateUrl: './checkout-page.html',
  styleUrl: './checkout-page.css',
})
export class CheckoutPageComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  readonly cartStore = inject(CartStore);
  readonly checkoutStore = inject(CheckoutStore);
  private readonly orderStore = inject(OrderStore);
  private readonly authStore = inject(AuthStore);
  private readonly notifications = inject(NotificationService);

  private pollTimer: ReturnType<typeof setTimeout> | null = null;

  // Accordion state
  activeSection = signal<'address' | 'shipping' | 'summary' | 'payment'>('address');
  
  readonly ChevronIcon = ChevronDown;
  readonly CheckIcon = CheckCircle;
  readonly CardIcon = CreditCard;
  readonly TruckIcon = Truck;
  readonly MapPinIcon = MapPin;
  readonly BagIcon = ShoppingBag;

  constructor() {
    // React to SignalR order status updates — no polling needed.
    // The SignalR update IS the source of truth for order status.
    // We build a minimal order directly from it (no BFF round-trip)
    // so the UI always reflects the real status, even if the BFF is down.
    effect(() => {
      const update = this.notifications.orderUpdates();
      if (update && this.checkoutStore.submitted()) {
        if (!this.checkoutStore.hasOrder()) {
          // First update — create order directly from SignalR data
          this.checkoutStore.setOrder({
            id: update.orderId,
            buyerId: update.buyerId,
            status: update.status,
            totalAmount: 0,
            createdAt: update.timestamp,
            completedAt: null,
            items: [],
          });
          this.stopPolling();

          // Enrich with full order data from BFF (items, amounts) in background.
          // Non-blocking — the status is already displayed.
          this.orderStore.loadOrderById(update.orderId).then(() => {
            const full = this.orderStore.selectedOrder();
            if (full) {
              this.checkoutStore.setOrder(full);
            }
          }).catch(() => { /* BFF enrichment is optional */ });
        } else {
          // Subsequent update — update the status in-place so the UI
          // transitions through Submitted → InventoryReserved → Completed
          const currentOrder = this.checkoutStore.order();
          if (currentOrder && currentOrder.id === update.orderId) {
            this.checkoutStore.setOrder({ ...currentOrder, status: update.status });
          }
        }
      }
    });
  }

  ngOnInit(): void {
    // Only reset if there's no in-flight checkout. If an order is already
    // tracked (from SignalR or fallback poll) or submission is in progress,
    // preserve the state so the user sees the status after navigation.
    if (!this.checkoutStore.hasOrder() && !this.checkoutStore.submitted()) {
      this.checkoutStore.reset();
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

  onConfirm(): void {
    // Fire checkout without blocking UI — the endpoint returns 202 Accepted
    // almost immediately. SignalR delivers the real order status updates.
    // submitCheckout() sets submitted=true internally on success.
    this.checkoutStore.submitCheckout().then(() => {
      // If the HTTP call returned an error, submitCheckout already set
      // submitted=false in its catch block, so the user can retry.
    });

    // Fallback: poll after 15s in case SignalR misses the update
    this.pollTimer = setTimeout(async () => {
      if (this.checkoutStore.hasOrder()) return;

      try {
        const buyerId = this.authStore.user()?.id || '';
        if (buyerId) {
          await this.orderStore.loadOrders(buyerId);
          const orders = this.orderStore.orders();
          const correlationId = this.cartStore.checkoutCorrelationId();
          const order = correlationId
            ? orders.find((o) => o.id === correlationId)
            : orders[0];
          if (order) {
            this.checkoutStore.setOrder(order);
            return;
          }
        }
      } catch { /* ignore */ }

      this.checkoutStore.setPollingExpired(true);
    }, 15000);
  }

  retryCheckout(): void {
    this.checkoutStore.retryCheckout();
  }

  private stopPolling(): void {
    if (this.pollTimer) {
      clearTimeout(this.pollTimer);
      this.pollTimer = null;
    }
  }
}
