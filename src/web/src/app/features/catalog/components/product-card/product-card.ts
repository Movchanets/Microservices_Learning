import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { ProductListItem } from '../../catalog.models';

@Component({
  selector: 'app-product-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, RouterLink, LucideAngularModule],
  template: `
    <div
      [attr.data-testid]="'product-card-' + product().id"
      class="group flex flex-col bg-card border border-border
                rounded-2xl p-6 shadow-sm hover:shadow-md
                transition-all duration-300 h-full"
    >
      <!-- Image with defer -->
      <a
        [routerLink]="['/catalog', product().id]"
        class="block relative w-full aspect-square rounded-xl overflow-hidden mb-5 bg-muted/10"
      >
        @if (product().imageUrl) {
          @defer (on viewport) {
            <img
              [src]="product().imageUrl"
              [alt]="product().name"
              class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
              loading="lazy"
            />
          } @placeholder {
            <div class="w-full h-full flex items-center justify-center text-muted">
              <lucide-icon name="Package" class="w-12 h-12 opacity-30"></lucide-icon>
            </div>
          }
        } @else {
          <div class="w-full h-full flex items-center justify-center text-muted">
            <lucide-icon name="Package" class="w-12 h-12 opacity-30"></lucide-icon>
          </div>
        }

        <!-- Category Badge -->
        <span
          class="absolute top-3 left-3 px-3 py-1 bg-card border border-border shadow-sm
                     rounded-full text-xs font-medium text-foreground"
        >
          {{ product().categoryName }}
        </span>

        <!-- In Stock Badge -->
        @if (product().status === 'Active') {
          <span
            class="absolute top-3 right-3 px-2 py-0.5 bg-emerald-50 text-emerald-700
                       border-emerald-200 dark:bg-emerald-950/50 dark:text-emerald-300
                       dark:border-emerald-800 rounded-full text-xs font-medium border"
          >
            In Stock
          </span>
        }
      </a>

      <!-- Content -->
      <div class="flex flex-col flex-1">
        <h3
          class="text-xl font-bold text-foreground font-lexend mb-1 line-clamp-2
                   group-hover:text-primary transition-colors"
        >
          <a [routerLink]="['/catalog', product().id]">{{ product().name }}</a>
        </h3>

        <!-- SKU -->
        <p class="text-xs text-muted mb-4 font-mono">
          <lucide-icon name="Tag" class="w-3 h-3 inline mr-1"></lucide-icon>
          {{ product().sku }}
        </p>

        <!-- Spacer pushes price to bottom -->
        <div class="flex-1"></div>

        <!-- Footer: Price & Add to Cart -->
        <div class="flex items-center justify-between mt-4">
          <div class="flex flex-col">
            <span class="text-2xl font-bold text-foreground font-lexend">
              {{ product().price | currency: product().currency : 'symbol' : '1.2-2' }}
            </span>
          </div>

          <button
            (click)="addToCart.emit(product().id)"
            class="flex items-center justify-center w-12 h-12 rounded-xl
                         bg-primary text-white hover:bg-secondary
                         active:scale-95 transition-all"
            aria-label="Add to cart"
            data-testid="add-to-cart-btn"
          >
            <lucide-icon name="ShoppingCart" class="w-5 h-5"></lucide-icon>
          </button>
        </div>
      </div>
    </div>
  `,
})
export class ProductCardComponent {
  product = input.required<ProductListItem>();
  addToCart = output<string>();
}
