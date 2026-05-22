import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface CategoryTree {
  id: string;
  name: string;
  description: string | null;
  parentCategoryId: string | null;
  slug: string;
  sortOrder: number;
  isActive: boolean;
  children: CategoryTree[];
}

@Injectable({
  providedIn: 'root'
})
export class CategoryTreeService {
  private readonly http = inject(HttpClient);
  
  // Public signal holding the tree
  readonly categoryTree = signal<CategoryTree[]>([]);
  readonly loading = signal<boolean>(false);
  
  async initialize(): Promise<void> {
    try {
      this.loading.set(true);
      const tree = await firstValueFrom(
        this.http.get<CategoryTree[]>('/api/catalog/categories/tree', { withCredentials: true })
      );
      this.categoryTree.set(tree);
    } catch (error) {
      console.error('Failed to load category tree', error);
      this.categoryTree.set([]);
    } finally {
      this.loading.set(false);
    }
  }
}
