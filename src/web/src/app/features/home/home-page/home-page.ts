import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { HomeStore } from '../home.store';
import { CartStore } from '../../cart/cart.store';
import { RecentlyViewedService } from '../../../core/services/recently-viewed.service';
import { CategoryTilesComponent } from '../components/category-tiles/category-tiles';
import { ProductCarouselComponent } from '../components/product-carousel/product-carousel';
import { ProductListItem } from '../../catalog/catalog.models';

@Component({
  selector: 'app-home-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CategoryTilesComponent,
    ProductCarouselComponent,
  ],
  template: `
    <div class="min-h-screen bg-background">
      <div class="container mx-auto px-4 py-8 flex flex-col gap-12">
        <!-- Hero Section -->
        <section class="text-center py-16">
          <h1 class="text-4xl md:text-5xl font-bold text-foreground font-lexend mb-4">
            Welcome to Marketplace
          </h1>
          <p class="text-lg text-muted-foreground max-w-2xl mx-auto">
            Discover the best products from trusted sellers. Secure, fast, and transparent.
          </p>
        </section>

        <!-- Category Tiles -->
        @if (homeStore.categories().length > 0) {
          <section>
            <h2 class="text-2xl font-bold text-foreground font-lexend mb-6">Shop by Category</h2>
            <app-category-tiles [categories]="homeStore.categories()" />
          </section>
        }

        <!-- Featured Products Carousel -->
        @if (homeStore.featuredProducts().length > 0) {
          <section>
            <app-product-carousel
              title="Featured Products"
              [products]="homeStore.featuredProducts()"
              viewAllLink="/catalog"
              (addToCart)="onAddToCart($event)"
            />
          </section>
        }

        <!-- New Arrivals Carousel -->
        @if (homeStore.newArrivals().length > 0) {
          <section>
            <app-product-carousel
              title="New Arrivals"
              [products]="homeStore.newArrivals()"
              viewAllLink="/catalog"
              (addToCart)="onAddToCart($event)"
            />
          </section>
        }

        <!-- Recently Viewed -->
        @if (recentlyViewedService.recentlyViewed().length > 0) {
          <section>
            <h2 class="text-2xl font-bold text-foreground font-lexend mb-6">Recently Viewed</h2>
            <div class="text-muted-foreground text-sm">
              {{ recentlyViewedService.recentlyViewed().length }} items
            </div>
          </section>
        }
      </div>
    </div>
  `,
})
export class HomePageComponent implements OnInit {
  protected homeStore = inject(HomeStore);
  protected recentlyViewedService = inject(RecentlyViewedService);
  private cartStore = inject(CartStore);

  ngOnInit(): void {
    this.homeStore.loadAll();
  }

  onAddToCart(product: ProductListItem): void {
    if (product.defaultSkuId && product.defaultSkuCode) {
      this.cartStore.addToCart(product.id, product.defaultSkuId, product.defaultSkuCode, 1);
    }
  }
}
