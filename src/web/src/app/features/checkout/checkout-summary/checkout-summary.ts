import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { CartStore } from '../../cart/cart.store';

@Component({
  selector: 'app-checkout-summary',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, LucideAngularModule],
  template: `
    <div class="bg-card rounded-3xl border border-border p-6">
      <h2 class="text-xl font-bold font-lexend mb-4">Order Summary</h2>

      <ul class="divide-y divide-border mb-6">
        @for (item of cartStore.items(); track item.productId) {
          <li class="py-3 flex items-center justify-between">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 bg-muted/20 rounded-lg flex items-center justify-center">
                <lucide-icon name="Package" class="w-5 h-5 text-muted/50"></lucide-icon>
              </div>
              <div>
                <p class="font-medium text-sm">{{ item.productId }}</p>
                <p class="text-xs text-muted">Qty: {{ item.quantity }}</p>
              </div>
            </div>
            @if (item.price) {
              <span class="text-sm font-medium">{{ item.lineTotal | currency }}</span>
            }
          </li>
        }
      </ul>

      <div class="border-t border-border pt-4 flex items-center justify-between">
        <span class="text-muted font-medium">Total Items</span>
        <span class="text-2xl font-bold font-lexend">{{ cartStore.totalItems() }}</span>
      </div>
    </div>
  `,
})
export class CheckoutSummaryComponent {
  cartStore = inject(CartStore);
}
