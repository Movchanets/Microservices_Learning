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

  private pollTimer: ReturnType<typeof setInterval> | null = null;
  submitted = signal(false);

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
    await this.checkoutStore.submitCheckout();

    if (!this.checkoutStore.error()) {
      this.submitted.set(true);
      this.startPolling();
    }
  }

  private startPolling(): void {
    const buyerId = this.authStore.user()?.id || '';

    this.pollTimer = setInterval(async () => {
      if (buyerId) {
        await this.orderStore.loadOrders(buyerId);
      }
      const orders = this.orderStore.orders();
      const correlationId = this.cartStore.checkoutCorrelationId();

      const order = orders.find((o) => o.id === correlationId) ?? orders[0];

      if (order) {
        this.checkoutStore.setOrder(order);
        this.stopPolling();
      }
    }, 2000);

    setTimeout(() => this.stopPolling(), 30_000);
  }

  private stopPolling(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }
}
