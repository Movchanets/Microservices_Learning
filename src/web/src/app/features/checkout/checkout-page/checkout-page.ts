import { Component, ChangeDetectionStrategy, inject, OnInit, OnDestroy, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
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
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, LucideAngularModule, CheckoutSummaryComponent, CheckoutStatusComponent, AddressFormComponent],
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
  submitted = signal(false);
  pollingExpired = signal(false);

  // Accordion state
  activeSection = signal<'address' | 'shipping' | 'summary' | 'payment'>('address');
  
  readonly ChevronIcon = ChevronDown;
  readonly CheckIcon = CheckCircle;
  readonly CardIcon = CreditCard;
  readonly TruckIcon = Truck;
  readonly MapPinIcon = MapPin;
  readonly BagIcon = ShoppingBag;

  constructor() {
    // React to SignalR order status updates — no polling needed
    effect(() => {
      const update = this.notifications.orderUpdates();
      if (update && this.submitted() && !this.checkoutStore.hasOrder()) {
        // SignalR notified us of an order update — fetch the order directly
        this.orderStore.loadOrderById(update.orderId).then(() => {
          const order = this.orderStore.selectedOrder();
          if (order) {
            this.checkoutStore.setOrder(order);
            this.stopPolling();
          }
        });
      }
    });
  }

  ngOnInit(): void {
    this.checkoutStore.reset();
    this.submitted.set(false);
    this.pollingExpired.set(false);
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
    this.submitted.set(true);

    await this.checkoutStore.submitCheckout();

    if (this.checkoutStore.error()) {
      this.submitted.set(false);
      return;
    }

    // Start a single fallback check after 8s in case SignalR misses it
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

      this.pollingExpired.set(true);
    }, 8000);
  }

  private stopPolling(): void {
    if (this.pollTimer) {
      clearTimeout(this.pollTimer);
      this.pollTimer = null;
    }
  }
}
