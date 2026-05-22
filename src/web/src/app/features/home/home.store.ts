import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { inject } from '@angular/core';
import { ProductListItem, Category } from '../catalog/catalog.models';
import { CatalogService } from '../catalog/catalog.service';

interface HomeState {
  featuredProducts: ProductListItem[];
  newArrivals: ProductListItem[];
  categories: Category[];
  loading: boolean;
  error: string | null;
}

const initialState: HomeState = {
  featuredProducts: [],
  newArrivals: [],
  categories: [],
  loading: false,
  error: null,
};

export const HomeStore = signalStore(
  { providedIn: 'root' },
  withState<HomeState>(initialState),
  withMethods((store, catalogService = inject(CatalogService)) => {
    return {
      async loadFeatured(): Promise<void> {
        patchState(store, { loading: true, error: null });
        try {
          const products = await catalogService.getFeatured();
          patchState(store, { featuredProducts: products, loading: false });
        } catch {
          patchState(store, { error: 'Failed to load featured products', loading: false });
        }
      },

      async loadNewArrivals(): Promise<void> {
        try {
          const result = await catalogService.getProducts({ page: 1, pageSize: 8 });
          patchState(store, { newArrivals: result.items });
        } catch {
          // Non-critical; silently fail
        }
      },

      async loadCategories(): Promise<void> {
        try {
          const categories = await catalogService.getCategories();
          patchState(store, { categories: categories.filter(c => c.isActive).slice(0, 8) });
        } catch {
          // Non-critical; silently fail
        }
      },

      async loadAll(): Promise<void> {
        await Promise.all([
          this.loadFeatured(),
          this.loadNewArrivals(),
          this.loadCategories(),
        ]);
      },
    };
  }),
);
