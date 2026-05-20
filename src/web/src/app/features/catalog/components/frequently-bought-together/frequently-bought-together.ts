import { Component, ChangeDetectionStrategy, input, signal, computed, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { ProductListItem } from '../../catalog.models';
import { CartStore } from '../../../cart/cart.store';

@Component({
  selector: 'app-frequently-bought-together',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, RouterLink, LucideAngularModule],
  template: `
    @if (loading()) {
      <div class="p-6 bg-card border border-border rounded-2xl animate-pulse">
        <div class="h-6 bg-muted/20 rounded w-48 mb-4"></div>
        <div class="flex gap-4">
          <div class="h-32 w-32 bg-muted/20 rounded-xl"></div>
          <div class="h-32 w-32 bg-muted/20 rounded-xl"></div>
          <div class="h-32 w-32 bg-muted/20 rounded-xl"></div>
        </div>
      </div>
    } @else if (products().length > 0) {
      <div class="p-6 bg-card border border-border rounded-2xl">
        <h3 class="text-xl font-bold text-foreground font-lexend mb-4">
          Frequently Bought Together
        </h3>

        <!-- Product Cards -->
        <div class="flex flex-wrap gap-4 mb-6">
          @for (product of products(); track product.id; let i = $index) {
            <div class="flex items-center gap-2">
              @if (i > 0) {
                <lucide-icon name="Plus" class="w-4 h-4 text-muted-foreground"></lucide-icon>
              }
              <a
                [routerLink]="['/catalog', product.id]"
                class="group flex flex-col items-center p-3 bg-muted/10 rounded-xl
                       hover:bg-muted/20 transition-colors w-32"
              >
                @if (product.imageUrl) {
                  <img
                    [src]="product.imageUrl"
                    [alt]="product.name"
                    class="w-20 h-20 object-cover rounded-lg mb-2"
                  />
                } @else {
                  <div class="w-20 h-20 flex items-center justify-center bg-muted/20 rounded-lg mb-2">
                    <lucide-icon name="Package" class="w-8 h-8 text-muted opacity-30"></lucide-icon>
                  </div>
                }
                <span class="text-xs text-foreground text-center line-clamp-2 group-hover:text-primary transition-colors">
                  {{ product.name }}
                </span>
                <span class="text-xs text-muted-foreground mt-1">
                  {{ product.price | currency: product.currency : 'symbol' : '1.2-2' }}
                </span>
              </a>
            </div>
          }
        </div>

        <!-- Bundle Price & Add All -->
        <div class="flex items-center justify-between pt-4 border-t border-border">
          <div class="text-lg text-muted-foreground">
            Bundle price:
            <span class="text-xl font-bold text-foreground ml-2">
              {{ totalPrice() | currency: bundleCurrency() : 'symbol' : '1.2-2' }}
            </span>
          </div>
          <button
            (click)="addAllToCart()"
            [disabled]="cartStore.loading()"
            class="px-6 py-3 bg-primary text-white font-semibold rounded-xl
                   hover:bg-secondary active:scale-[0.98] transition-all
                   flex items-center gap-2 shadow-lg
                   disabled:opacity-50 disabled:cursor-not-allowed"
          >
            @if (cartStore.loading()) {
              <lucide-icon name="Loader" class="w-5 h-5 animate-spin"></lucide-icon>
              Adding...
            } @else {
              <lucide-icon name="ShoppingCart" class="w-5 h-5"></lucide-icon>
              Add All {{ products().length }} to Cart
            }
          </button>
        </div>
      </div>
    }
  `,
})
export class FrequentlyBoughtTogetherComponent {
  protected cartStore = inject(CartStore);

  products = input.required<ProductListItem[]>();
  loading = input(false);

  protected totalPrice = computed(() =>
    this.products().reduce((sum, p) => sum + p.price, 0),
  );

  protected bundleCurrency = computed(() =>
    this.products().length > 0 ? this.products()[0].currency : 'USD',
  );

  async addAllToCart(): Promise<void> {
    for (const product of this.products()) {
      await this.cartStore.addToCart(product.id, 1);
    }
  }
}
