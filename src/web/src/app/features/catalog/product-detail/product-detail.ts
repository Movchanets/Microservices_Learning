import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { CatalogService } from '../catalog.service';
import { Product, ProductListItem } from '../catalog.models';
import { InventoryService } from '../../../core/services/inventory.service';
import { BuyBoxComponent } from '../components/buy-box/buy-box';
import { FrequentlyBoughtTogetherComponent } from '../components/frequently-bought-together/frequently-bought-together';
import { StockIndicatorComponent } from '../../../shared/components/stock-indicator/stock-indicator';

// TODO: Add product variant selector (color, size) when Catalog supports variants.
//       Ref: plans/future_design/product_details.md — "Advanced Product Variations Selector"

// TODO: Add Community Q&A and Reviews section.
//       Ref: plans/future_design/product_details.md — "Community Q&A and Rich Reviews"

@Component({
  selector: 'app-product-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    LucideAngularModule,
    BuyBoxComponent,
    FrequentlyBoughtTogetherComponent,
    StockIndicatorComponent,
  ],
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
          <div class="grid grid-cols-1 lg:grid-cols-[1fr,400px] gap-8 lg:gap-12">
            <!-- Left: Image + Description -->
            <div>
              <!-- Image Gallery -->
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

              <!-- Product Info -->
              <div class="mt-8">
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
                  class="text-3xl md:text-4xl font-bold text-foreground font-lexend mb-4 leading-tight"
                >
                  {{ p.name }}
                </h1>

                <!-- Stock Status (inline for mobile) -->
                <div class="lg:hidden mb-4">
                  <app-stock-indicator
                    [quantity]="stockQuantity()"
                    [loading]="stockLoading()"
                  />
                </div>

                <div class="prose prose-invert max-w-none text-muted-foreground mb-8">
                  <p class="text-lg leading-relaxed">{{ p.description }}</p>
                </div>

                <!-- Tags -->
                @if (p.tags && p.tags.length > 0) {
                  <div class="flex flex-wrap gap-2">
                    @for (tag of p.tags; track tag) {
                      <span
                        class="px-3 py-1.5 bg-muted/20 border border-border/50 rounded-lg text-sm text-muted-foreground"
                      >
                        {{ tag }}
                      </span>
                    }
                  </div>
                }
              </div>
            </div>

            <!-- Right: Sticky Buy Box -->
            <div class="lg:sticky lg:top-6 lg:self-start">
              <app-buy-box
                [sku]="p.sku"
                [price]="p.price"
                [currency]="p.currency"
                [stockQuantity]="stockQuantity()"
                [stockLoading]="stockLoading()"
                (buyNow)="onBuyNow()"
              />
            </div>
          </div>

          <!-- Frequently Bought Together -->
          <div class="mt-12">
            <app-frequently-bought-together
              [products]="recommendations()"
              [loading]="recommendationsLoading()"
            />
          </div>
        }
      </div>
    </div>
  `,
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private catalogService = inject(CatalogService);
  private inventoryService = inject(InventoryService);

  product = signal<Product | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  stockQuantity = signal<number | null>(null);
  stockLoading = signal(false);

  recommendations = signal<ProductListItem[]>([]);
  recommendationsLoading = signal(false);

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

      // Load stock and recommendations in parallel
      this.loadStock(p.sku);
      this.loadRecommendations(p.id);
    } catch (err: any) {
      this.error.set(err?.error?.error ?? 'Failed to load product details');
    } finally {
      this.loading.set(false);
    }
  }

  private async loadStock(sku: string): Promise<void> {
    this.stockLoading.set(true);
    try {
      const item = await this.inventoryService.checkStock(sku);
      this.stockQuantity.set(item.availableQuantity);
    } catch {
      // If inventory check fails, assume unknown (null) — don't block the user
      this.stockQuantity.set(null);
    } finally {
      this.stockLoading.set(false);
    }
  }

  private async loadRecommendations(productId: string): Promise<void> {
    this.recommendationsLoading.set(true);
    try {
      const items = await this.catalogService.getRecommendations(productId);
      this.recommendations.set(items);
    } catch {
      // Recommendations are non-critical; silently fail
      this.recommendations.set([]);
    } finally {
      this.recommendationsLoading.set(false);
    }
  }

  onBuyNow(): void {
    this.router.navigate(['/checkout']);
  }
}
