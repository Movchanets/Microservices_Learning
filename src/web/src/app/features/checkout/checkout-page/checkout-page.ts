import { Component, ChangeDetectionStrategy, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule, ChevronDown, CheckCircle, CreditCard, Truck, MapPin, ShoppingBag } from 'lucide-angular';
import { CartStore } from '../../cart/cart.store';
import { CheckoutStore } from '../checkout.store';
import { OrderStore } from '../../orders/order.store';
import { AuthStore } from '../../../core/auth/auth.store';
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

  private pollTimer: ReturnType<typeof setTimeout> | null = null;
  submitted = signal(false);
  pollingExpired = signal(false);
  private polling = false;

  // Accordion state
  activeSection = signal<'address' | 'shipping' | 'summary' | 'payment'>('address');
  
  readonly ChevronIcon = ChevronDown;
  readonly CheckIcon = CheckCircle;
  readonly CardIcon = CreditCard;
  readonly TruckIcon = Truck;
  readonly MapPinIcon = MapPin;
  readonly BagIcon = ShoppingBag;

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
    // Set submitted BEFORE the async call to prevent race condition
    // where cart empties but submitted is still false → "empty cart" flash
    this.submitted.set(true);
    this.startPolling();

    await this.checkoutStore.submitCheckout();

    if (this.checkoutStore.error()) {
      this.submitted.set(false);
      this.stopPolling();
    }
  }

  private startPolling(): void {
    const buyerId = this.authStore.user()?.id || '';
    let attempts = 0;
    const maxAttempts = 15;

    const poll = async () => {
      if (this.polling) return; // Skip if previous request still in-flight
      this.polling = true;
      attempts++;

      try {
        if (buyerId) {
          await this.orderStore.loadOrders(buyerId);
        }

        const orders = this.orderStore.orders();
        const correlationId = this.cartStore.checkoutCorrelationId();

        const order = correlationId
          ? orders.find((o) => o.id === correlationId)
          : orders[0];

        if (order) {
          this.checkoutStore.setOrder(order);
          this.stopPolling();
          return;
        }
      } catch {
        // Transient error — continue polling
      } finally {
        this.polling = false;
      }

      if (attempts >= maxAttempts) {
        this.pollingExpired.set(true);
        this.stopPolling();
        return;
      }

      // Schedule next poll AFTER current one completed
      this.pollTimer = setTimeout(poll, 2000);
    };

    // First poll after short delay
    this.pollTimer = setTimeout(poll, 1000);
  }

  private stopPolling(): void {
    this.polling = false;
    if (this.pollTimer) {
      clearTimeout(this.pollTimer);
      this.pollTimer = null;
    }
  }
}
