import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { CatalogService } from '../catalog.service';
import { Product } from '../catalog.models';
import { CartStore } from '../../cart/cart.store';
// TODO: Create InventoryService to check stock availability before adding to cart.
//       Ref: src/Microservices/Inventory/Inventory.API/Endpoints/InventoryEndpoints.cs

// TODO: Add "Sticky Buy Box" — keep Add to Cart button pinned when scrolling.
//       Ref: plans/future_design/product_details.md — "Sticky Buy Box" section

// TODO: Add "Frequently Bought Together" section below product details.
//       Shows 2-3 complementary items with "Add all to Cart" button.
//       Ref: plans/future_design/product_details.md — "Frequently Bought Together" section

// TODO: Add stock availability check before adding to cart.
//       Call Inventory.API to check available quantity.
//       Show "Only X left in stock" warning when low.
//       Ref: src/Microservices/Inventory/Inventory.API/Endpoints/InventoryEndpoints.cs

// TODO: Add product variant selector (color, size) when Catalog supports variants.
//       Ref: plans/future_design/product_details.md — "Advanced Product Variations Selector"

// TODO: Add Community Q&A and Reviews section.
//       Ref: plans/future_design/product_details.md — "Community Q&A and Rich Reviews"

@Component({
  selector: 'app-product-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, RouterLink, LucideAngularModule],
  template: `
    <div class="min-h-screen bg-background p-6 pt-10">
      <div class="container mx-auto max-w-6xl">
        <!-- Back Link -->
        <a
          routerLink="/catalog"
          class="inline-flex items-center text-muted hover:text-primary transition-colors mb-8"
        >
          <lucide-icon name="ChevronLeft" class="w-4 h-4 mr-1"></lucide-icon>
          Back to Catalog
        </a>

        @if (loading()) {
          <!-- Skeleton Detail -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-12 animate-pulse">
            <div class="aspect-square bg-muted/20 rounded-3xl"></div>
            <div class="space-y-6 pt-4">
              <div class="h-6 bg-muted/20 rounded-md w-32"></div>
              <div class="h-10 bg-muted/20 rounded-lg w-3/4"></div>
              <div class="h-16 bg-muted/20 rounded-lg w-1/3"></div>
              <div class="space-y-3">
                <div class="h-4 bg-muted/20 rounded-md w-full"></div>
                <div class="h-4 bg-muted/20 rounded-md w-5/6"></div>
              </div>
              <div class="h-14 bg-muted/20 rounded-xl w-48 mt-8"></div>
            </div>
          </div>
        } @else if (error()) {
          <div class="py-24 text-center">
            <p class="text-xl text-red-400 mb-4">{{ error() }}</p>
            <a routerLink="/catalog" class="text-primary hover:underline">Return to Catalog</a>
          </div>
        } @else if (product(); as p) {
          <div class="grid grid-cols-1 md:grid-cols-2 gap-12">
            <!-- Left: Image Gallery -->
            <div
              class="bg-card/40 backdrop-blur-sm border border-border rounded-3xl p-4 md:p-8 flex items-center justify-center min-h-[400px]"
            >
              @if (p.imageUrl) {
                <img
                  [src]="p.imageUrl"
                  [alt]="p.name"
                  class="w-full max-w-md rounded-2xl object-cover shadow-lg"
                />
              } @else {
                <lucide-icon name="Package" class="w-32 h-32 text-muted opacity-30"></lucide-icon>
              }
            </div>

            <!-- Right: Details -->
            <div class="flex flex-col pt-2 md:pt-8">
              <!-- Breadcrumb / Category -->
              <div class="flex items-center gap-2 mb-4">
                <span class="px-3 py-1 bg-primary/10 text-primary rounded-full text-sm font-medium">
                  {{ p.categoryName }}
                </span>
                <span class="text-muted text-sm font-mono flex items-center">
                  <lucide-icon name="Tag" class="w-3 h-3 mr-1"></lucide-icon>
                  {{ p.sku }}
                </span>
              </div>

              <h1
                class="text-4xl md:text-5xl font-bold text-foreground font-lexend mb-4 leading-tight"
              >
                {{ p.name }}
              </h1>

              <div class="text-4xl font-bold text-foreground font-lexend mb-8">
                {{ p.price | currency: p.currency : 'symbol' : '1.2-2' }}
              </div>

              <div class="prose prose-invert max-w-none text-muted-foreground mb-8">
                <p class="text-lg leading-relaxed">{{ p.description }}</p>
              </div>

              <!-- Tags -->
              @if (p.tags && p.tags.length > 0) {
                <div class="flex flex-wrap gap-2 mb-8">
                  @for (tag of p.tags; track tag) {
                    <span
                      class="px-3 py-1.5 bg-muted/20 border border-border/50 rounded-lg text-sm text-muted-foreground"
                    >
                      {{ tag }}
                    </span>
                  }
                </div>
              }

              <div class="mt-auto">
                <button
                  (click)="onAddToCart(p.id)"
                  class="w-full md:w-auto px-10 py-4 bg-primary text-white text-lg font-bold rounded-xl
                               hover:bg-secondary active:scale-[0.98] transition-all
                               flex items-center justify-center gap-3 shadow-xl shadow-primary/20"
                >
                  <lucide-icon name="ShoppingCart" class="w-6 h-6"></lucide-icon>
                  Add to Cart
                </button>
              </div>
            </div>
          </div>
        }
      </div>
    </div>
  `,
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private catalogService = inject(CatalogService);
  private cartStore = inject(CartStore);

  product = signal<Product | null>(null);
  loading = signal<boolean>(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadProduct(id);
    } else {
      this.error.set('Product ID missing in URL');
      this.loading.set(false);
    }
  }

  private async loadProduct(id: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const p = await this.catalogService.getProduct(id);
      this.product.set(p);
    } catch (err: any) {
      this.error.set(err?.error?.error ?? 'Failed to load product details');
    } finally {
      this.loading.set(false);
    }
  }

  onAddToCart(productId: string): void {
    const p = this.product();
    if (p) {
      this.cartStore.addToCart(p.sku, 1);
    }
  }
}
