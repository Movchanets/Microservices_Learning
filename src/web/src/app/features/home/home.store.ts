import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ProductListItem, Category } from '../catalog/catalog.models';

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
  withMethods((store) => {
    const http = inject(HttpClient);

    return {
      async loadFeatured(): Promise<void> {
        patchState(store, { loading: true, error: null });
        try {
          const products = await firstValueFrom(
            http.get<ProductListItem[]>('/api/catalog/products/featured'),
          );
          patchState(store, { featuredProducts: products, loading: false });
        } catch {
          patchState(store, { error: 'Failed to load featured products', loading: false });
        }
      },

      async loadNewArrivals(): Promise<void> {
        try {
          const result = await firstValueFrom(
            http.get<{ items: ProductListItem[] }>('/api/catalog/products', {
              params: { page: 1, pageSize: 8, sort: 'newest' },
            }),
          );
          patchState(store, { newArrivals: result.items });
        } catch {
          // Non-critical; silently fail
        }
      },

      async loadCategories(): Promise<void> {
        try {
          const categories = await firstValueFrom(
            http.get<Category[]>('/api/catalog/categories'),
          );
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
