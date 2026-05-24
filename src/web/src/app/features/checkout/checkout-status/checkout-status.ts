import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { OrderStatus } from '../checkout.models';

@Component({
  selector: 'app-checkout-status',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule],
  template: `
    <div class="bg-card rounded-3xl border border-border p-8 text-center">
      @switch (status()) {
        @case ('Submitted') {
          <div class="animate-pulse">
            <lucide-icon name="Clock" class="w-16 h-16 mx-auto mb-4 text-primary"></lucide-icon>
            <h2 class="text-2xl font-bold font-lexend mb-2">Order Submitted</h2>
            <p class="text-muted mb-6">Your order is being processed...</p>
          </div>
        }
        @case ('InventoryReserved') {
          <lucide-icon name="PackageCheck" class="w-16 h-16 mx-auto mb-4 text-primary"></lucide-icon>
          <h2 class="text-2xl font-bold font-lexend mb-2">Inventory Reserved</h2>
          <p class="text-muted mb-6">Items are reserved. Processing payment...</p>
        }
        @case ('PaymentProcessing') {
          <div class="animate-pulse">
            <lucide-icon name="CreditCard" class="w-16 h-16 mx-auto mb-4 text-primary"></lucide-icon>
            <h2 class="text-2xl font-bold font-lexend mb-2">Processing Payment</h2>
            <p class="text-muted mb-6">Waiting for payment confirmation...</p>
          </div>
        }
        @case ('Processing') {
          <div class="animate-pulse">
            <lucide-icon name="Loader" class="w-16 h-16 mx-auto mb-4 text-primary"></lucide-icon>
            <h2 class="text-2xl font-bold font-lexend mb-2">Processing Order</h2>
            <p class="text-muted mb-6">Your order is being processed...</p>
          </div>
        }
        @case ('Completed') {
          <lucide-icon name="CheckCircle2" class="w-16 h-16 mx-auto mb-4 text-green-500"></lucide-icon>
          <h2 data-testid="checkout-status-completed" class="text-2xl font-bold font-lexend mb-2">Order Completed!</h2>
          <p class="text-muted mb-6">Your order has been placed successfully.</p>
          <a routerLink="/orders"
             class="inline-block px-6 py-3 bg-primary text-white rounded-xl font-medium hover:bg-secondary transition-colors">
            View Orders
          </a>
        }
        @case ('Cancelled') {
          <lucide-icon name="XCircle" class="w-16 h-16 mx-auto mb-4 text-red-500"></lucide-icon>
          <h2 data-testid="checkout-status-cancelled" class="text-2xl font-bold font-lexend mb-2">Order Cancelled</h2>
          <p class="text-muted mb-6">{{ error() || 'Your order could not be completed.' }}</p>
          <div class="flex flex-col sm:flex-row items-center justify-center gap-3">
            <button (click)="retry.emit()"
                    data-testid="checkout-retry-cancelled"
                    class="px-6 py-3 bg-primary text-white rounded-xl font-medium hover:bg-secondary transition-colors">
              Try Again
            </button>
            <a routerLink="/catalog"
               class="px-6 py-3 border border-border text-foreground rounded-xl font-medium hover:bg-muted/10 transition-colors">
              Continue Shopping
            </a>
          </div>
        }
        @case ('Faulted') {
          <lucide-icon name="AlertTriangle" class="w-16 h-16 mx-auto mb-4 text-red-500"></lucide-icon>
          <h2 data-testid="checkout-status-faulted" class="text-2xl font-bold font-lexend mb-2">Something Went Wrong</h2>
          <p class="text-muted mb-6">{{ error() || 'An error occurred while processing your order.' }}</p>
          <div class="flex flex-col sm:flex-row items-center justify-center gap-3">
            <button (click)="retry.emit()"
                    data-testid="checkout-retry-faulted"
                    class="px-6 py-3 bg-primary text-white rounded-xl font-medium hover:bg-secondary transition-colors">
              Try Again
            </button>
            <a routerLink="/cart"
               class="px-6 py-3 border border-border text-foreground rounded-xl font-medium hover:bg-muted/10 transition-colors">
              Back to Cart
            </a>
          </div>
        }
        @default {
          <lucide-icon name="HelpCircle" class="w-16 h-16 mx-auto mb-4 text-muted"></lucide-icon>
          <h2 class="text-2xl font-bold font-lexend mb-2">Unknown Status</h2>
          <p class="text-muted mb-6">Order status: {{ status() }}</p>
          <a routerLink="/orders"
             class="inline-block px-6 py-3 bg-primary text-white rounded-xl font-medium hover:bg-secondary transition-colors">
            View Orders
          </a>
        }
      }
    </div>
  `,
})
export class CheckoutStatusComponent {
  status = input.required<OrderStatus>();
  correlationId = input<string | null>(null);
  error = input<string | null>(null);
  retry = output<void>();
}
