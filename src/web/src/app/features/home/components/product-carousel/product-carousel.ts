import { Component, ChangeDetectionStrategy, ElementRef, input, output, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { ProductListItem } from '../../../catalog/catalog.models';
import { ProductCardComponent } from '../../../catalog/components/product-card/product-card';

@Component({
  selector: 'app-product-carousel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule, ProductCardComponent],
  template: `
    <div class="relative">
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <h2 class="text-2xl font-bold text-foreground font-lexend">{{ title() }}</h2>
        @if (viewAllLink()) {
          <a [routerLink]="viewAllLink()" class="text-primary hover:text-primary/80 text-sm font-medium">
            View All
          </a>
        }
      </div>

      <!-- Carousel -->
      <div class="relative group">
        <!-- Scroll Container -->
        <div
          #scrollContainer
          class="flex gap-4 overflow-x-auto scroll-smooth pb-4 scrollbar-hide"
          style="scrollbar-width: none; -ms-overflow-style: none;"
        >
          @for (product of products(); track product.id) {
            <div class="flex-none w-[250px]">
              <app-product-card [product]="product" (addToCart)="addToCart.emit($event)" />
            </div>
          } @empty {
            @for (i of [1,2,3,4]; track i) {
              <div class="flex-none w-[250px] h-[350px] bg-muted/10 rounded-2xl animate-pulse"></div>
            }
          }
        </div>

        <!-- Left Arrow -->
        <button
          (click)="scrollLeft()"
          class="absolute left-0 top-1/2 -translate-y-1/2 -translate-x-2 p-2
                 bg-card border border-border rounded-full
                 text-foreground opacity-0 group-hover:opacity-100 transition-opacity
                 hover:bg-muted/20"
          aria-label="Scroll left"
        >
          <lucide-icon name="ChevronLeft" class="w-5 h-5"></lucide-icon>
        </button>

        <!-- Right Arrow -->
        <button
          (click)="scrollRight()"
          class="absolute right-0 top-1/2 -translate-y-1/2 translate-x-2 p-2
                 bg-card border border-border rounded-full
                 text-foreground opacity-0 group-hover:opacity-100 transition-opacity
                 hover:bg-muted/20"
          aria-label="Scroll right"
        >
          <lucide-icon name="ChevronRight" class="w-5 h-5"></lucide-icon>
        </button>
      </div>
    </div>
  `,
})
export class ProductCarouselComponent {
  title = input.required<string>();
  products = input.required<ProductListItem[]>();
  viewAllLink = input<string | null>(null);
  addToCart = output<ProductListItem>();

  scrollContainer = viewChild<ElementRef<HTMLDivElement>>('scrollContainer');

  scrollLeft(): void {
    this.scrollContainer()?.nativeElement.scrollBy({ left: -280, behavior: 'smooth' });
  }

  scrollRight(): void {
    this.scrollContainer()?.nativeElement.scrollBy({ left: 280, behavior: 'smooth' });
  }
}
