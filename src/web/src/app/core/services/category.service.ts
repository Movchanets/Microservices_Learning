// Category service.
// Fetches categories from the Catalog API for product form dropdowns.
// Caches the result — categories rarely change during a session.

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface CategoryOption {
  id: string;
  name: string;
  parentCategoryId: string | null;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/catalog/categories';
  private cachedCategories: CategoryOption[] | null = null;

  async getCategories(): Promise<CategoryOption[]> {
    if (this.cachedCategories) return this.cachedCategories;

    const categories = await firstValueFrom(
      this.http.get<CategoryOption[]>(this.baseUrl)
    );
    this.cachedCategories = categories;
    return categories;
  }
}
