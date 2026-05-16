import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { CatalogService } from '../catalog.service';
import { Product, ProductListItem, CreateReviewRequest } from '../catalog.models';
import { InventoryService } from '../../../core/services/inventory.service';
import { AuthStore } from '../../../core/auth/auth.store';
import { ReviewStore } from '../review.store';
import { BuyBoxComponent } from '../components/buy-box/buy-box';
import { FrequentlyBoughtTogetherComponent } from '../components/frequently-bought-together/frequently-bought-together';
import { StockIndicatorComponent } from '../../../shared/components/stock-indicator/stock-indicator';
import { ReviewSummaryComponent } from '../components/review-summary/review-summary';
import { ReviewListComponent } from '../components/review-list/review-list';
import { WriteReviewComponent } from '../components/write-review/write-review';

// TODO: Add product variant selector (color, size) when Catalog supports variants.
//       Ref: plans/future_design/product_details.md — "Advanced Product Variations Selector"

@Component({
  selector: 'app-product-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    LucideAngularModule,
    BuyBoxComponent,
    FrequentlyBoughtTogetherComponent,
    StockIndicatorComponent,
    ReviewSummaryComponent,
    ReviewListComponent,
    WriteReviewComponent,
  ],
  providers: [ReviewStore],
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

          <!-- Reviews Section -->
          <div class="mt-16">
            <h2 class="text-2xl font-bold text-foreground font-lexend mb-8">Customer Reviews</h2>

            <div class="grid grid-cols-1 lg:grid-cols-[300px,1fr] gap-8">
              <!-- Left: Summary -->
              <div>
                @if (reviewStore.summary(); as summary) {
                  <app-review-summary
                    [summary]="summary"
                    (filterByRating)="onFilterByRating($event)"
                  />
                }
              </div>

              <!-- Right: Reviews List -->
              <div class="flex flex-col gap-6">
                <!-- Sort & Filter Bar -->
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <span class="text-sm text-muted-foreground">Sort by:</span>
                    <select
                      (change)="onSortChange($event)"
                      class="px-3 py-1.5 bg-muted/10 border border-border rounded-lg text-sm text-foreground
                             focus:outline-none focus:border-primary"
                    >
                      <option value="helpful">Most Helpful</option>
                      <option value="newest">Newest</option>
                      <option value="highest">Highest Rated</option>
                      <option value="lowest">Lowest Rated</option>
                    </select>
                  </div>

                  @if (authStore.user()) {
                    <app-write-review
                      [submitting]="reviewStore.submitting()"
                      (submit)="onSubmitReview($event)"
                    />
                  }
                </div>

                <!-- Reviews -->
                @if (reviewStore.loading()) {
                  <div class="space-y-4 animate-pulse">
                    @for (i of [1, 2, 3]; track i) {
                      <div class="h-40 bg-muted/20 rounded-2xl"></div>
                    }
                  </div>
                } @else {
                  <app-review-list
                    [reviews]="reviewStore.reviews()"
                    (vote)="onVote($event)"
                  />
                }
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
  private router = inject(Router);
  private catalogService = inject(CatalogService);
  private inventoryService = inject(InventoryService);
  protected authStore = inject(AuthStore);

  protected reviewStore = inject(ReviewStore);

  product = signal<Product | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  stockQuantity = signal<number | null>(null);
  stockLoading = signal(false);

  recommendations = signal<ProductListItem[]>([]);
  recommendationsLoading = signal(false);

  private currentProductId = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.currentProductId = id;
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

      // Load stock, recommendations, and reviews in parallel
      this.loadStock(p.sku);
      this.loadRecommendations(p.id);
      this.loadReviews(p.id);
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
      this.recommendations.set([]);
    } finally {
      this.recommendationsLoading.set(false);
    }
  }

  private loadReviews(productId: string): void {
    this.reviewStore.loadSummary(productId);
    this.reviewStore.loadReviews(productId);
  }

  onBuyNow(): void {
    this.router.navigate(['/checkout']);
  }

  onSortChange(event: Event): void {
    const sort = (event.target as HTMLSelectElement).value;
    this.reviewStore.setSort(this.currentProductId, sort);
  }

  onFilterByRating(rating: number): void {
    const current = this.reviewStore.ratingFilter();
    this.reviewStore.setRatingFilter(this.currentProductId, current === rating ? null : rating);
  }

  onVote(event: { reviewId: string; isHelpful: boolean }): void {
    this.reviewStore.voteReview(this.currentProductId, event.reviewId, event.isHelpful);
  }

  async onSubmitReview(data: CreateReviewRequest): Promise<void> {
    await this.reviewStore.createReview(this.currentProductId, data);
  }
}
