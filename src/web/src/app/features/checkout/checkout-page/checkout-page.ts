import { Component, ChangeDetectionStrategy, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { CartStore } from '../../cart/cart.store';
import { CheckoutStore } from '../checkout.store';
import { OrderStore } from '../../orders/order.store';
import { CheckoutSummaryComponent } from '../checkout-summary/checkout-summary';
import { CheckoutStatusComponent } from '../checkout-status/checkout-status';

// TODO: Add shipping address form before order confirmation.
//       OrderSubmittedEvent now expects: ShippingAddress, ShippingCity, ShippingPostalCode, ShippingCountry.
//       Ref: src/BuildingBlocks/SharedContracts/Events/Cart/OrderSubmittedEvent.cs
//       Ref: plans/future_design/cart_and_checkout.md — "Single-Page Checkout" section
//       Implementation: Add address form fields (street, city, postalCode, country) above the confirm button.
//       Pass address data to checkout store which sends it with the checkout request.

// TODO: Add express checkout options (Apple Pay, Google Pay) above the standard confirm button.
//       Ref: plans/future_design/cart_and_checkout.md — "Express Checkout Options" section

// TODO: Add free shipping progress bar. Show "Add $X more to unlock free shipping!" message.
//       Ref: plans/future_design/cart_and_checkout.md — "Slide-out Cart Drawer" section

@Component({
  selector: 'app-checkout-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule, CheckoutSummaryComponent, CheckoutStatusComponent],
  template: `
    <div class="min-h-screen bg-background p-6 pt-10">
      <div class="container mx-auto max-w-3xl">
        <h1 class="text-3xl font-bold text-foreground font-lexend mb-8">Checkout</h1>

        @if (checkoutStore.hasOrder()) {
          <app-checkout-status
            [status]="checkoutStore.orderStatus()!"
            [correlationId]="cartStore.checkoutCorrelationId()" />
        } @else if (submitted()) {
          <div class="bg-card/60 backdrop-blur-sm rounded-3xl border border-border p-8 text-center">
            <div class="animate-pulse">
              <lucide-icon name="Clock" class="w-16 h-16 mx-auto mb-4 text-primary"></lucide-icon>
              <h2 class="text-2xl font-bold font-lexend mb-2">Order Submitted</h2>
              <p class="text-muted mb-6">Your order is being created...</p>
            </div>
            <div class="mt-4">
              <p class="text-sm text-muted mb-2">Correlation ID</p>
              <p class="font-mono text-xs text-muted bg-muted/10 inline-block px-3 py-1 rounded-lg">
                {{ cartStore.checkoutCorrelationId() }}
              </p>
            </div>
          </div>
        } @else if (cartStore.isEmpty()) {
          <div class="text-center py-16 bg-card/60 backdrop-blur-sm rounded-3xl border border-border">
            <lucide-icon name="ShoppingCart" class="w-16 h-16 mx-auto mb-4 opacity-30"></lucide-icon>
            <p class="text-xl font-medium text-foreground mb-4">Your cart is empty</p>
            <a href="/catalog"
               class="inline-block px-6 py-3 bg-primary text-white rounded-xl hover:bg-secondary transition-colors">
              Continue Shopping
            </a>
          </div>
        } @else {
          <app-checkout-summary />

          @if (checkoutStore.error()) {
            <div class="mt-4 p-4 bg-red-500/10 text-red-500 rounded-xl">
              {{ checkoutStore.error() }}
            </div>
          }

          <div class="mt-6 flex items-center justify-between">
            <a href="/cart" class="text-muted hover:text-foreground transition-colors flex items-center gap-2">
              <lucide-icon name="ChevronLeft" class="w-4 h-4"></lucide-icon>
              Back to Cart
            </a>
            <button (click)="onConfirm()"
                    [disabled]="checkoutStore.submitting()"
                    class="px-8 py-3 bg-success text-white rounded-xl font-medium hover:opacity-90 transition-all disabled:opacity-50 cursor-pointer">
              @if (checkoutStore.submitting()) {
                <span class="flex items-center gap-2">
                  <span class="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full"></span>
                  Processing...
                </span>
              } @else {
                Confirm Order
              }
            </button>
          </div>
        }
      </div>
    </div>
  `,
})
export class CheckoutPageComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  readonly cartStore = inject(CartStore);
  readonly checkoutStore = inject(CheckoutStore);
  private readonly orderStore = inject(OrderStore);

  private pollTimer: ReturnType<typeof setInterval> | null = null;
  submitted = signal(false);

  ngOnInit(): void {
    this.checkoutStore.reset();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  async onConfirm(): Promise<void> {
    await this.checkoutStore.submitCheckout();

    if (!this.checkoutStore.error()) {
      this.submitted.set(true);
      this.startPolling();
    }
  }

  private startPolling(): void {
    const buyerId = localStorage.getItem('buyerId') || 'guest-user';

    this.pollTimer = setInterval(async () => {
      await this.orderStore.loadOrders(buyerId);
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
