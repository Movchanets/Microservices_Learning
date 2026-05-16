import { Component, ChangeDetectionStrategy, inject, OnInit, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { computed } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { CatalogStore } from '../catalog.store';
import { CartStore } from '../../cart/cart.store';
import { ProductCardComponent } from '../components/product-card/product-card';
import { PaginationComponent } from '../components/pagination/pagination';
import { SearchFacetsComponent } from '../components/search-facets/search-facets';

@Component({
  selector: 'app-product-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [CatalogStore], // Feature-scoped store
  imports: [
    FormsModule,
    LucideAngularModule,
    ProductCardComponent,
    PaginationComponent,
    SearchFacetsComponent,
  ],
  template: `
    <div class="min-h-screen bg-background p-6 pt-10">
      <div class="container mx-auto">
        <!-- Header -->
        <header class="mb-10">
          <h1
            class="text-4xl font-bold text-foreground font-lexend mb-2"
            data-testid="catalog-title"
          >
            Explore Catalog
          </h1>
          <p class="text-muted text-lg max-w-2xl">
            Discover premium products from verified sellers.
          </p>
        </header>

        <!-- Search bar (local overrides header if used) -->
        <div class="relative max-w-xl mb-8">
          <lucide-icon
            name="Search"
            class="w-5 h-5 absolute left-4 top-1/2 -translate-y-1/2 text-muted pointer-events-none"
          >
          </lucide-icon>
          <input
            type="text"
            [ngModel]="store.searchQuery()"
            (ngModelChange)="onSearch($event)"
            placeholder="Search products..."
            class="w-full pl-12 pr-4 py-3 rounded-xl border border-border
                        focus:ring-2 focus:ring-primary focus:border-transparent outline-none
                        bg-card/60 backdrop-blur-sm text-foreground placeholder:text-muted"
            data-testid="search-input"
          />
        </div>

        <!-- Content: sidebar + grid -->
        <div class="flex flex-col lg:flex-row gap-8">
          <!-- Sidebar: Search Facets (search mode only, category sidebar removed) -->
          <div class="lg:w-64 shrink-0 space-y-4">
            @if (store.isSearchMode()) {
              <app-search-facets
                [categoryFacets]="searchCategoryFacets()"
                [priceMin]="store.priceMin()"
                [priceMax]="store.priceMax()"
                (priceRangeChange)="onPriceRangeChange($event)"
                (categoryClicked)="onFacetCategoryClick($event)"
                (clearFilters)="onClearFilters()"
              />
            }
          </div>

          <!-- Product grid -->
          <div class="flex-1">
            @if (store.loading()) {
              <!-- Skeleton grid -->
              <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-6">
                @for (i of skeletons; track i) {
                  <div
                    class="bg-card/40 backdrop-blur-sm border border-border rounded-2xl p-6 shadow-sm animate-pulse"
                  >
                    <div class="w-full aspect-square bg-muted/20 rounded-xl mb-4"></div>
                    <div class="h-3 bg-muted/20 rounded-md w-1/3 mb-3"></div>
                    <div class="h-5 bg-muted/20 rounded-md w-3/4 mb-6"></div>
                    <div class="h-8 bg-muted/20 rounded-xl w-full"></div>
                  </div>
                }
              </div>
            } @else if (store.error()) {
              <div class="py-16 text-center">
                <p class="text-lg text-red-400 mb-4">{{ store.error() }}</p>
                <button
                  (click)="store.refresh()"
                  class="px-6 py-2 bg-primary text-white rounded-xl hover:bg-secondary transition-colors"
                >
                  Try Again
                </button>
              </div>
            } @else {
              <!-- Results count -->
              <p class="text-sm text-muted mb-4">
                {{ store.totalCount() }} product{{ store.totalCount() !== 1 ? 's' : '' }} found
              </p>

              <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-6">
                @for (product of store.products(); track product.id) {
                  <app-product-card [product]="product" (addToCart)="onAddToCart($event)" />
                } @empty {
                  <div class="col-span-full py-16 text-center text-muted">
                    <lucide-icon
                      name="Package"
                      class="w-16 h-16 mx-auto mb-4 opacity-30"
                    ></lucide-icon>
                    <p class="text-lg">No products found.</p>
                    @if (store.searchQuery()) {
                      <p class="text-sm mt-2">Try adjusting your search or filters.</p>
                    }
                  </div>
                }
              </div>

              <!-- Pagination -->
              <app-pagination
                [currentPage]="store.page()"
                [totalPages]="store.totalPages()"
                [hasPrevious]="store.hasPrevious()"
                [hasNext]="store.hasNext()"
                (pageChange)="onPageChange($event)"
              />
            }
          </div>
        </div>
      </div>
    </div>
  `,
})
export class ProductListComponent implements OnInit, OnDestroy {
  store = inject(CatalogStore);
  cartStore = inject(CartStore);
  route = inject(ActivatedRoute);
  
  readonly skeletons = Array.from({ length: 6 }, (_, i) => i);
  private searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;
  private sub: Subscription | null = null;

  ngOnInit(): void {
    // Note: We no longer load side-bar categories here since it's global mega-menu now.
    
    this.sub = this.route.queryParams.subscribe(params => {
      const q = params['q'] || '';
      const categoryId = params['categoryId'] || null;
      
      this.store.updateSearchQuery(q);
      this.store.selectCategory(categoryId);
      this.store.refresh();
    });
  }

  ngOnDestroy(): void {
    if (this.sub) {
      this.sub.unsubscribe();
    }
  }

  onSearch(query: string): void {
    this.store.updateSearchQuery(query);
    if (this.searchDebounceTimer) clearTimeout(this.searchDebounceTimer);
    this.searchDebounceTimer = setTimeout(() => this.store.refresh(), 350);
  }

  onPageChange(page: number): void {
    this.store.goToPage(page);
    this.store.refresh();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  onAddToCart(productId: string): void {
    const product = this.store.products().find((p) => p.id === productId);
    if (product) {
      this.cartStore.addToCart(product.sku, 1, product.price);
    }
  }

  searchCategoryFacets = computed(() => {
    const facets = this.store.facets();
    return facets['categories'] ?? [];
  });

  onPriceRangeChange(event: { min: number | null; max: number | null }): void {
    this.store.setPriceRange(event.min, event.max);
    this.store.refresh();
  }

  onFacetCategoryClick(categoryName: string): void {
    const currentQuery = this.store.searchQuery();
    const newQuery = currentQuery.includes(categoryName)
      ? currentQuery
      : `${currentQuery} ${categoryName}`.trim();
    this.store.updateSearchQuery(newQuery);
    this.store.refresh();
  }

  onClearFilters(): void {
    this.store.updateSearchQuery('');
    this.store.selectCategory(null);
    this.store.setPriceRange(null, null);
    this.store.refresh();
  }
}
