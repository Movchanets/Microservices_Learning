import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  Product,
  ProductListItem,
  PagedResult,
  Category,
  ProductListParams,
  ProductSearchParams,
  SearchResult,
  VariantMatrix,
} from './catalog.models';
import { buildParams } from '../../core/utils/http.utils';

/**
 * All calls route through the YARP API Gateway (BFF).
 * - /api/catalog/* → catalog-api
 * - /api/search/*  → search-api
 *
 * The apiInterceptor adds withCredentials: true automatically.
 */
@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);

  // ── Catalog CRUD (via /api/catalog) ─────────────

  getProducts(params: ProductListParams = {}): Promise<PagedResult<ProductListItem>> {
    const httpParams = buildParams({
      page: params.page,
      pageSize: params.pageSize,
      categoryId: params.categoryId,
      storeId: params.storeId,
      search: params.search,
    });

    return firstValueFrom(
      this.http.get<PagedResult<ProductListItem>>('/api/catalog/products', { params: httpParams }),
    );
  }

  getProduct(id: string): Promise<Product> {
    return firstValueFrom(this.http.get<Product>(`/bff/catalog/products/${id}`));
  }

  /**
   * Fetches the variant matrix for a product.
   * Returns all possible SKU combinations based on variant-axis attribute definitions.
   * Used by the variant picker to render color × storage grids.
   */
  getVariantMatrix(productId: string): Promise<VariantMatrix> {
    return firstValueFrom(
      this.http.get<VariantMatrix>(`/api/catalog/products/${productId}/variant-matrix`),
    );
  }

  // ── Categories (via /api/catalog) ───────────────

  getCategories(): Promise<Category[]> {
    return firstValueFrom(this.http.get<Category[]>('/api/catalog/categories'));
  }

  getFeatured(tag?: string): Promise<ProductListItem[]> {
    const params: Record<string, string> = tag ? { tag } : {};
    return firstValueFrom(
      this.http.get<ProductListItem[]>('/api/catalog/products/featured', { params }),
    );
  }

  // ── Full-text Search (via /api/search) ──────────

  searchProducts(params: ProductSearchParams = {}): Promise<SearchResult<ProductListItem>> {
    const httpParams = buildParams({
      q: params.q,
      categoryId: params.categoryId,
      priceMin: params.priceMin,
      priceMax: params.priceMax,
      tags: params.tags,
      brand: params.brand,
      minRating: params.minRating,
      inStock: params.inStock,
      page: params.page,
      pageSize: params.pageSize,
    });

    return firstValueFrom(
      this.http.get<SearchResult<ProductListItem>>('/api/search/products', { params: httpParams }),
    );
  }
}
