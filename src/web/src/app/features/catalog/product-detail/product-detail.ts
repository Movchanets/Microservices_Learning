import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { CatalogService } from '../catalog.service';
import { Product } from '../catalog.models';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, RouterLink, LucideAngularModule],
  template: `
    <div class="min-h-screen bg-background p-6 pt-10">
      <div class="container mx-auto max-w-6xl">

        <!-- Back link -->
        <a routerLink="/catalog"
           class="inline-flex items-center gap-2 text-muted hover:text-foreground transition-colors mb-8 group">
          <lucide-icon name="ChevronLeft" class="w-4 h-4 transition-transform group-hover:-translate-x-1"></lucide-icon>
          Back to catalog
        </a>

        @if (loading()) {
          <!-- Skeleton -->
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-12 animate-pulse">
            <div class="aspect-square bg-muted/20 rounded-2xl"></div>
            <div class="space-y-4">
              <div class="h-4 bg-muted/20 rounded-md w-1/4"></div>
              <div class="h-8 bg-muted/20 rounded-md w-3/4"></div>
              <div class="h-10 bg-muted/20 rounded-md w-1/3 mt-6"></div>
              <div class="h-4 bg-muted/20 rounded-md w-full mt-8"></div>
              <div class="h-4 bg-muted/20 rounded-md w-5/6"></div>
              <div class="h-4 bg-muted/20 rounded-md w-4/6"></div>
            </div>
          </div>
        } @else if (error()) {
          <div class="py-20 text-center">
            <p class="text-lg text-red-400 mb-4">{{ error() }}</p>
            <a routerLink="/catalog"
               class="px-6 py-2 bg-primary text-white rounded-xl hover:bg-secondary transition-colors inline-block">
              Back to catalog
            </a>
          </div>
        } @else if (product()) {
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-12">

            <!-- Image -->
            <div class="aspect-square bg-card/40 backdrop-blur-sm border border-border rounded-2xl overflow-hidden">
              @if (product()!.imageUrl) {
                <img [src]="product()!.imageUrl"
                     [alt]="product()!.name"
                     class="object-cover w-full h-full" />
              } @else {
                <div class="w-full h-full flex items-center justify-center">
                  <lucide-icon name="Package" class="w-24 h-24 text-muted/20"></lucide-icon>
                </div>
              }
            </div>

            <!-- Info panel -->
            <div class="flex flex-col">
              <!-- Category -->
              <span class="text-sm font-medium text-primary/70 uppercase tracking-wider mb-2">
                {{ product()!.categoryName }}
              </span>

              <!-- Name -->
              <h1 class="text-3xl font-bold text-foreground font-lexend mb-2">
                {{ product()!.name }}
              </h1>

              <!-- SKU -->
              <span class="text-xs text-muted font-mono">
                SKU: {{ product()!.sku }}
              </span>

              <!-- Price -->
              <div class="mt-6 mb-8">
                <span class="text-4xl font-bold text-foreground">
                  {{ product()!.price | currency:product()!.currency }}
                </span>
              </div>

              <!-- Tags -->
              @if (product()!.tags.length > 0) {
                <div class="flex flex-wrap gap-2 mb-6">
                  @for (tag of product()!.tags; track tag) {
                    <span class="px-3 py-1 bg-primary/10 text-primary text-xs font-medium rounded-full flex items-center gap-1">
                      <lucide-icon name="Tag" class="w-3 h-3"></lucide-icon>
                      {{ tag }}
                    </span>
                  }
                </div>
              }

              <!-- Description -->
              <div class="mb-8">
                <h2 class="font-lexend font-semibold text-foreground mb-3">Description</h2>
                <p class="text-muted leading-relaxed whitespace-pre-line">
                  {{ product()!.description }}
                </p>
              </div>

              <!-- CTA -->
              <div class="mt-auto flex gap-4">
                <button (click)="onAddToCart()"
                        class="flex-1 bg-primary hover:bg-secondary text-white py-3 px-6
                               rounded-xl font-medium transition-colors flex items-center justify-center gap-2
                               active:scale-[0.98]">
                  <lucide-icon name="ShoppingCart" class="w-5 h-5"></lucide-icon>
                  Add to Cart
                </button>
              </div>

              <!-- Meta -->
              <div class="mt-6 pt-6 border-t border-border text-xs text-muted space-y-1">
                <p>Status: <span class="font-medium">{{ product()!.status }}</span></p>
                <p>Listed: {{ product()!.createdAt | date:'mediumDate' }}</p>
                @if (product()!.updatedAt) {
                  <p>Updated: {{ product()!.updatedAt | date:'mediumDate' }}</p>
                }
              </div>
            </div>
          </div>
        }
      </div>
    </div>
  `
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private catalogService = inject(CatalogService);

  product = signal<Product | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Product ID not provided');
      this.loading.set(false);
      return;
    }

    try {
      const product = await this.catalogService.getProduct(id);
      this.product.set(product);
    } catch (err: any) {
      this.error.set(err?.status === 404 ? 'Product not found' : 'Failed to load product');
    } finally {
      this.loading.set(false);
    }
  }

  onAddToCart(): void {
    const p = this.product();
    if (p) {
      // TODO: Phase 7.3 — Cart integration
      console.log('Add to cart:', p.id);
    }
  }
}
