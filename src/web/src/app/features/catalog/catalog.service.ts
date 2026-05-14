import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  Product,
  ProductListItem,
  PagedResult,
  Category,
  ProductListParams,
  ProductSearchParams,
  SearchResult,
} from './catalog.models';

/**
 * All calls route through the YARP API Gateway (BFF).
 * - /api/catalog/* → catalog-api
 * - /api/search/*  → search-api
 *
 * The apiInterceptor adds withCredentials: true automatically.
 */
@Injectable({ providedIn: 'root' })
export class CatalogService {
  private http = inject(HttpClient);

  // ── Catalog CRUD (via /api/catalog) ─────────────

  getProducts(params: ProductListParams = {}): Promise<PagedResult<ProductListItem>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    if (params.sellerId) httpParams = httpParams.set('sellerId', params.sellerId);
    if (params.search) httpParams = httpParams.set('search', params.search);

    return firstValueFrom(
      this.http.get<PagedResult<ProductListItem>>('/api/catalog/products', { params: httpParams }),
    );
  }

  getProduct(id: string): Promise<Product> {
    return firstValueFrom(this.http.get<Product>(`/api/catalog/products/${id}`));
  }

  // ── Categories (via /api/catalog) ───────────────

  getCategories(): Promise<Category[]> {
    return firstValueFrom(this.http.get<Category[]>('/api/catalog/categories'));
  }

  // ── Full-text Search (via /api/search) ──────────

  searchProducts(params: ProductSearchParams = {}): Promise<SearchResult<ProductListItem>> {
    let httpParams = new HttpParams();
    if (params.q) httpParams = httpParams.set('q', params.q);
    if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    if (params.priceMin != null) httpParams = httpParams.set('priceMin', params.priceMin);
    if (params.priceMax != null) httpParams = httpParams.set('priceMax', params.priceMax);
    if (params.tags) httpParams = httpParams.set('tags', params.tags);
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);

    return firstValueFrom(
      this.http.get<SearchResult<ProductListItem>>('/api/search/products', { params: httpParams }),
    );
  }
}
