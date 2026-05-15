import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { CartStore } from '../cart.store';

@Component({
  selector: 'app-cart-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule],
  template: `
    <div class="min-h-screen bg-background p-6 pt-10">
      <div class="container mx-auto max-w-4xl">
        <h1 class="text-3xl font-bold text-foreground font-lexend mb-8">Your Cart</h1>

        @if (store.loading() && store.isEmpty()) {
          <div class="flex justify-center p-12">
            <div
              class="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full"
            ></div>
          </div>
        } @else if (store.error()) {
          <div class="p-4 bg-red-500/10 text-red-500 rounded-xl mb-6">
            {{ store.error() }}
          </div>
        } @else if (store.isEmpty()) {
          <div
            class="text-center py-16 bg-card/60 backdrop-blur-sm rounded-3xl border border-border"
          >
            <lucide-icon
              name="ShoppingCart"
              class="w-16 h-16 mx-auto mb-4 opacity-30"
            ></lucide-icon>
            <p class="text-xl font-medium text-foreground mb-4">Your cart is empty</p>
            <a
              routerLink="/catalog"
              class="inline-block px-6 py-3 bg-primary text-white rounded-xl hover:bg-secondary transition-colors"
            >
              Continue Shopping
            </a>
          </div>
        } @else {
          <div class="bg-card/60 backdrop-blur-sm rounded-3xl border border-border overflow-hidden">
            <ul class="divide-y divide-border">
              @for (item of store.items(); track item.sku) {
                <li class="p-6 flex items-center gap-6">
                  <!-- In a real app, you would fetch product details by SKU here -->
                  <div class="w-20 h-20 bg-muted/20 rounded-xl flex items-center justify-center">
                    <lucide-icon name="Package" class="w-8 h-8 text-muted/50"></lucide-icon>
                  </div>

                  <div class="flex-1">
                    <h3 class="font-lexend font-medium text-lg">{{ item.sku }}</h3>
                    <p class="text-muted text-sm">Quantity: {{ item.quantity }}</p>
                  </div>

                  <div class="flex items-center gap-3">
                    <button
                      (click)="store.updateQuantity(item.sku, item.quantity - 1)"
                      class="p-2 hover:bg-muted/20 rounded-lg transition-colors"
                      [disabled]="store.loading()"
                    >
                      <lucide-icon name="Minus" class="w-4 h-4"></lucide-icon>
                    </button>
                    <span class="w-8 text-center font-medium">{{ item.quantity }}</span>
                    <button
                      (click)="store.updateQuantity(item.sku, item.quantity + 1)"
                      class="p-2 hover:bg-muted/20 rounded-lg transition-colors"
                      [disabled]="store.loading()"
                    >
                      <lucide-icon name="Plus" class="w-4 h-4"></lucide-icon>
                    </button>
                  </div>

                  <button
                    (click)="store.removeFromCart(item.sku)"
                    class="p-3 text-red-500 hover:bg-red-500/10 rounded-xl transition-colors ml-4"
                    [disabled]="store.loading()"
                  >
                    <lucide-icon name="Trash2" class="w-5 h-5"></lucide-icon>
                  </button>
                </li>
              }
            </ul>

            <div class="p-6 bg-muted/5 border-t border-border flex items-center justify-between">
              <div>
                <p class="text-muted mb-1">Total Items</p>
                <p class="text-2xl font-bold font-lexend">{{ store.totalItems() }}</p>
              </div>
              <button
                (click)="onCheckout()"
                [disabled]="store.loading()"
                class="px-8 py-3 bg-primary text-white rounded-xl font-medium hover:bg-secondary transition-colors disabled:opacity-50"
              >
                Checkout
              </button>
            </div>
          </div>

        }
      </div>
    </div>
  `,
})
export class CartPageComponent {
  private readonly router = inject(Router);
  readonly store = inject(CartStore);

  onCheckout() {
    this.router.navigate(['/checkout']);
  }
}
