import { Component, ChangeDetectionStrategy, input, output, signal, computed, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { StockIndicatorComponent } from '../../../../shared/components/stock-indicator/stock-indicator';
import { CartStore } from '../../../cart/cart.store';

@Component({
  selector: 'app-buy-box',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, LucideAngularModule, StockIndicatorComponent],
  template: `
    <div class="buy-box flex flex-col gap-4 p-6 bg-card border border-border rounded-2xl">
      <!-- Price -->
      <div class="text-3xl font-bold text-foreground font-lexend">
        {{ price() | currency: currency() : 'symbol' : '1.2-2' }}
      </div>

      <!-- Stock Status -->
      <app-stock-indicator
        [quantity]="stockQuantity()"
        [loading]="stockLoading()"
      />

      <!-- Quantity Selector -->
      @if (stockQuantity() === null || stockQuantity()! > 0) {
        <div class="flex items-center gap-3">
          <span class="text-sm text-muted-foreground">Qty:</span>
          <div class="flex items-center border border-border rounded-lg overflow-hidden">
            <button
              (click)="decrement()"
              [disabled]="quantity() <= 1"
              class="px-3 py-2 text-foreground hover:bg-muted/20 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
              aria-label="Decrease quantity"
            >
              <lucide-icon name="Minus" class="w-4 h-4"></lucide-icon>
            </button>
            <span class="px-4 py-2 text-foreground font-medium min-w-[3rem] text-center border-x border-border">
              {{ quantity() }}
            </span>
            <button
              (click)="increment()"
              [disabled]="quantity() >= maxQuantity()"
              class="px-3 py-2 text-foreground hover:bg-muted/20 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
              aria-label="Increase quantity"
            >
              <lucide-icon name="Plus" class="w-4 h-4"></lucide-icon>
            </button>
          </div>
        </div>
      }

      <!-- Add to Cart Button -->
      <button
        (click)="onAddToCart()"
        [disabled]="isOutOfStock() || cartStore.loading()"
        class="w-full py-4 px-6 bg-primary text-white text-lg font-bold rounded-xl
               hover:bg-secondary active:scale-[0.98] transition-all
               flex items-center justify-center gap-3 shadow-lg
               disabled:opacity-50 disabled:cursor-not-allowed disabled:active:scale-100"
      >
        @if (cartStore.loading()) {
          <lucide-icon name="Loader" class="w-5 h-5 animate-spin"></lucide-icon>
          Adding...
        } @else if (isOutOfStock()) {
          <lucide-icon name="XCircle" class="w-5 h-5"></lucide-icon>
          Out of Stock
        } @else {
          <lucide-icon name="ShoppingCart" class="w-5 h-5"></lucide-icon>
          Add to Cart
        }
      </button>

      <!-- Buy Now Button -->
      @if (!isOutOfStock()) {
        <button
          (click)="onBuyNow()"
          [disabled]="cartStore.loading()"
          class="w-full py-3 px-6 bg-secondary text-white font-semibold rounded-xl
                 hover:bg-secondary active:scale-[0.98] transition-all
                 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Buy Now
        </button>
      }
    </div>
  `,
})
export class BuyBoxComponent {
  protected cartStore = inject(CartStore);

  sku = input.required<string>();
  price = input.required<number>();
  currency = input.required<string>();
  stockQuantity = input<number | null>(null);
  stockLoading = input(false);
  sellerId = input<string>();

  buyNow = output<void>();

  quantity = signal(1);

  protected maxQuantity = computed(() => {
    const stock = this.stockQuantity();
    return stock === null ? 99 : Math.min(stock, 99);
  });

  protected isOutOfStock = computed(() => {
    return this.stockQuantity() !== null && this.stockQuantity() === 0;
  });

  increment(): void {
    this.quantity.update(q => Math.min(q + 1, this.maxQuantity()));
  }

  decrement(): void {
    this.quantity.update(q => Math.max(q - 1, 1));
  }

  async onAddToCart(): Promise<void> {
    await this.cartStore.addToCart(this.sku(), this.quantity(), this.sellerId());
  }

  onBuyNow(): void {
    this.cartStore.addToCart(this.sku(), this.quantity(), this.sellerId());
    this.buyNow.emit();
  }
}
