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
    <a [routerLink]="['/catalog', product().id]"
       class="group relative flex flex-col bg-card/60 backdrop-blur-sm
              rounded-2xl shadow-sm hover:shadow-xl transition-all duration-300
              overflow-hidden border border-border cursor-pointer
              scale-100 hover:scale-[1.02]">

      <!-- Image with defer -->
      <div class="aspect-square bg-muted/10 overflow-hidden">
        @defer (on viewport) {
          @if (product().imageUrl) {
            <img [src]="product().imageUrl"
                 [alt]="product().name"
                 class="object-cover w-full h-full transition-transform duration-500 group-hover:scale-110"
                 loading="lazy" />
          } @else {
            <div class="w-full h-full flex items-center justify-center bg-muted/10">
              <lucide-icon name="Package" class="w-16 h-16 text-muted/30"></lucide-icon>
            </div>
          }
        } @placeholder {
          <div class="w-full h-full animate-pulse bg-muted/20"></div>
        }
      </div>

      <div class="p-5 flex-1 flex flex-col">
        <!-- Category badge -->
        <span class="text-xs font-medium text-primary/70 uppercase tracking-wider mb-1">
          {{ product().categoryName }}
        </span>

        <h3 class="font-lexend text-lg font-medium text-foreground line-clamp-1">
          {{ product().name }}
        </h3>

        <div class="mt-auto pt-4 flex items-center justify-between">
          <span class="text-xl font-semibold text-foreground">
            {{ product().price | currency:product().currency }}
          </span>
          <button (click)="addToCart.emit(product().id); $event.preventDefault()"
                  class="bg-primary hover:bg-secondary text-white p-2.5 rounded-xl
                         transition-colors active:scale-95"
                  aria-label="Add to cart">
            <lucide-icon name="ShoppingCart" class="w-5 h-5"></lucide-icon>
          </button>
        </div>
      </div>
    </a>
  `
})
export class ProductCardComponent {
  product = input.required<ProductListItem>();
  addToCart = output<string>();
}
