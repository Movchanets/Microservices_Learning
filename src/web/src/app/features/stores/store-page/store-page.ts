import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { StoreService } from '../../seller-dashboard/store.service';
import { StoreSettings } from '../../seller-dashboard/seller.models';
import { CatalogService } from '../../catalog/catalog.service';
import { ProductListItem } from '../../catalog/catalog.models';
import { ProductCardComponent } from '../../catalog/components/product-card/product-card';
import { CartStore } from '../../cart/cart.store';

@Component({
  selector: 'app-store-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule, ProductCardComponent],
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
          <div class="animate-pulse space-y-8">
            <div class="h-8 bg-muted/20 rounded-lg w-48"></div>
            <div class="h-4 bg-muted/20 rounded-lg w-96"></div>
            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 mt-8">
              @for (i of [1, 2, 3]; track i) {
                <div class="h-64 bg-muted/20 rounded-2xl"></div>
              }
            </div>
          </div>
        } @else if (error()) {
          <div class="py-24 text-center">
            <p class="text-xl text-red-400 mb-4">{{ error() }}</p>
            <a routerLink="/catalog" class="text-primary hover:underline">Return to Catalog</a>
          </div>
        } @else if (store(); as s) {
          <!-- Store Header -->
          <div class="bg-card border border-border rounded-2xl p-8 mb-8">
            <div class="flex items-center gap-4 mb-4">
              @if (s.logoUrl) {
                <img [src]="s.logoUrl" [alt]="s.storeName" class="w-16 h-16 rounded-xl object-cover" />
              } @else {
                <div class="w-16 h-16 rounded-xl bg-muted/20 flex items-center justify-center">
                  <lucide-icon name="Store" class="w-8 h-8 text-muted/50"></lucide-icon>
                </div>
              }
              <div>
                <h1 class="text-2xl md:text-3xl font-bold text-foreground font-lexend">
                  {{ s.storeName }}
                </h1>
                @if (s.verificationStatus === 'Verified') {
                  <span class="inline-flex items-center gap-1 text-sm text-emerald-600 mt-1">
                    <lucide-icon name="CheckCircle" class="w-4 h-4"></lucide-icon>
                    Verified Seller
                  </span>
                }
              </div>
            </div>
            @if (s.description) {
              <p class="text-muted-foreground leading-relaxed">{{ s.description }}</p>
            }
          </div>

          <!-- Products -->
          <h2 class="text-xl font-bold text-foreground font-lexend mb-6">Products</h2>

          @if (products().length > 0) {
            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              @for (product of products(); track product.id) {
                <app-product-card
                  [product]="product"
                  (addToCart)="onAddToCart($event)"
                />
              }
            </div>
          } @else {
            <div class="py-16 text-center bg-card border border-border rounded-2xl">
              <lucide-icon name="Package" class="w-12 h-12 mx-auto mb-4 text-muted/30"></lucide-icon>
              <p class="text-muted-foreground">No products from this store yet.</p>
            </div>
          }
        }
      </div>
    </div>
  `,
})
export class StorePageComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private storeService = inject(StoreService);
  private catalogService = inject(CatalogService);
  private cartStore = inject(CartStore);

  store = signal<StoreSettings | null>(null);
  products = signal<ProductListItem[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadStore(id);
    } else {
      this.error.set('Store ID missing in URL');
      this.loading.set(false);
    }
  }

  private async loadStore(id: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const s = await this.storeService.getStoreById(id);
      this.store.set(s);

      // Load products for this store
      const result = await this.catalogService.getProducts({ storeId: id, pageSize: 50 });
      this.products.set(result.items);
    } catch (err: unknown) {
      const e = err as { error?: { error?: string } };
      this.error.set(e?.error?.error ?? 'Failed to load store');
    } finally {
      this.loading.set(false);
    }
  }

  onAddToCart(productId: string): void {
    this.cartStore.addToCart(productId, 1);
  }
}
