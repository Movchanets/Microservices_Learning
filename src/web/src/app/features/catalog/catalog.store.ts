import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { CatalogService } from './catalog.service';
import { ProductListItem, Category, FacetValue, PagedResult, SearchResult } from './catalog.models';
import { extractHttpError } from '../../core/utils/http.utils';

interface CatalogState {
  /** Product items from either Catalog.API or Search.API */
  products: ProductListItem[];
  /** Categories for sidebar/filter */
  categories: Category[];
  /** Search facets from Search.API */
  facets: Record<string, FacetValue[]>;
  /** Pagination */
  page: number;
  pageSize: number;
  totalCount: number;
  /** Filters */
  searchQuery: string;
  selectedCategoryId: string | null;
  priceMin: number | null;
  priceMax: number | null;
  selectedBrands: string[];
  minRating: number | null;
  inStockOnly: boolean;
  /** UI state */
  loading: boolean;
  error: string | null;
}

const initialState: CatalogState = {
  products: [],
  categories: [],
  facets: {},
  page: 1,
  pageSize: 20,
  totalCount: 0,
  searchQuery: '',
  selectedCategoryId: null,
  priceMin: null,
  priceMax: null,
  selectedBrands: [],
  minRating: null,
  inStockOnly: false,
  loading: false,
  error: null,
};

export const CatalogStore = signalStore(
  // Feature-scoped — NOT providedIn: 'root'
  withState(initialState),

  withComputed((store) => ({
    /** Total pages for paginator */
    totalPages: computed(() => Math.ceil(store.totalCount() / store.pageSize())),
    hasPrevious: computed(() => store.page() > 1),
    hasNext: computed(() => store.page() < Math.ceil(store.totalCount() / store.pageSize())),
    /** Whether we're in search mode (use Search.API) vs browse mode (use Catalog.API) */
    isSearchMode: computed(() =>
      store.searchQuery().trim().length > 0 ||
      store.selectedBrands().length > 0 ||
      store.minRating() !== null ||
      store.inStockOnly()
    ),
    /** Active category for UI highlighting */
    activeCategory: computed(
      () => store.categories().find((c) => c.id === store.selectedCategoryId()) ?? null,
    ),
  })),

  withMethods((store, catalogService = inject(CatalogService)) => ({
    /**
     * Load products from Catalog.API (browse mode).
     * Used when no search query is active.
     */
    async loadProducts(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const result: PagedResult<ProductListItem> = await catalogService.getProducts({
          page: store.page(),
          pageSize: store.pageSize(),
          categoryId: store.selectedCategoryId() ?? undefined,
        });
        patchState(store, {
          products: result.items,
          totalCount: result.totalCount,
          facets: {},
          loading: false,
        });
      } catch (err: unknown) {
        patchState(store, {
          error: extractHttpError(err, 'Failed to load products'),
          loading: false,
        });
      }
    },

    /**
     * Search products via Search.API (full-text mode).
     * Returns facets for category and price range filtering.
     */
    async searchProducts(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const brands = store.selectedBrands();
        const result: SearchResult<ProductListItem> = await catalogService.searchProducts({
          q: store.searchQuery(),
          categoryId: store.selectedCategoryId() ?? undefined,
          priceMin: store.priceMin() ?? undefined,
          priceMax: store.priceMax() ?? undefined,
          brand: brands.length === 1 ? brands[0] : undefined,
          minRating: store.minRating() ?? undefined,
          inStock: store.inStockOnly() || undefined,
          page: store.page(),
          pageSize: store.pageSize(),
        });
        patchState(store, {
          products: result.items,
          totalCount: result.totalCount,
          facets: result.facets ?? {},
          loading: false,
        });
      } catch (err: unknown) {
        patchState(store, {
          error: extractHttpError(err, 'Search failed'),
          loading: false,
        });
      }
    },

    /**
     * Load categories for the filter sidebar.
     */
    async loadCategories(): Promise<void> {
      try {
        const categories = await catalogService.getCategories();
        patchState(store, { categories });
      } catch {
        // Categories failing is non-critical
      }
    },

    /**
     * Smart refresh: decides Catalog.API vs Search.API based on search query.
     */
    async refresh(): Promise<void> {
      if (store.searchQuery().trim().length > 0) {
        await this.searchProducts();
      } else {
        await this.loadProducts();
      }
    },

    // ── Filter mutations ───────────────────────────

    updateSearchQuery(query: string): void {
      patchState(store, { searchQuery: query, page: 1 });
    },

    selectCategory(categoryId: string | null): void {
      patchState(store, { selectedCategoryId: categoryId, page: 1 });
    },

    setPriceRange(min: number | null, max: number | null): void {
      patchState(store, { priceMin: min, priceMax: max, page: 1 });
    },

    toggleBrand(brand: string): void {
      const current = store.selectedBrands();
      const updated = current.includes(brand)
        ? current.filter(b => b !== brand)
        : [...current, brand];
      patchState(store, { selectedBrands: updated, page: 1 });
    },

    setMinRating(rating: number | null): void {
      patchState(store, { minRating: rating, page: 1 });
    },

    setInStockOnly(inStock: boolean): void {
      patchState(store, { inStockOnly: inStock, page: 1 });
    },

    clearFilters(): void {
      patchState(store, {
        selectedCategoryId: null,
        priceMin: null,
        priceMax: null,
        selectedBrands: [],
        minRating: null,
        inStockOnly: false,
        page: 1,
      });
    },

    goToPage(page: number): void {
      patchState(store, { page });
    },
  })),
);
