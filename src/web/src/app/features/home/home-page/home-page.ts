import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { HomeStore } from '../home.store';
import { CartStore } from '../../cart/cart.store';
import { RecentlyViewedService } from '../../../core/services/recently-viewed.service';
import { HeroBannerComponent } from '../components/hero-banner/hero-banner';
import { CategoryTilesComponent } from '../components/category-tiles/category-tiles';
import { ProductCarouselComponent } from '../components/product-carousel/product-carousel';
import { DealOfTheDayComponent } from '../components/deal-of-the-day/deal-of-the-day';
import { ProductListItem } from '../../catalog/catalog.models';

@Component({
  selector: 'app-home-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    HeroBannerComponent,
    CategoryTilesComponent,
    ProductCarouselComponent,
    DealOfTheDayComponent,
  ],
  template: `
    <div class="min-h-screen bg-background">
      <div class="container mx-auto px-4 py-8 flex flex-col gap-12">
        <!-- Hero Banner -->
        <section>
          <app-hero-banner />
        </section>

        <!-- Category Tiles -->
        @if (homeStore.categories().length > 0) {
          <section>
            <h2 class="text-2xl font-bold text-foreground font-lexend mb-6">Shop by Category</h2>
            <app-category-tiles [categories]="homeStore.categories()" />
          </section>
        }

        <!-- Deal of the Day -->
        @if (homeStore.featuredProducts().length > 0) {
          <section>
            <app-deal-of-the-day [product]="homeStore.featuredProducts()[0]" />
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
