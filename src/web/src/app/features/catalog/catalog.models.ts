/**
 * Mirrors backend Catalog.Application.DTOs.ProductDto
 */
export interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  sku: string;
  categoryId: string;
  categoryName: string;
  status: ProductStatus;
  imageUrl: string | null;
  sellerId: string;
  tags: string[];
  createdAt: string;
  updatedAt: string | null;
}

/**
 * Mirrors backend Catalog.Application.DTOs.ProductListDto
 * Lighter payload for grid views (no description).
 */
export interface ProductListItem {
  id: string;
  name: string;
  price: number;
  currency: string;
  sku: string;
  categoryName: string;
  status: string;
  imageUrl: string | null;
  createdAt: string;
}

/**
 * Mirrors backend Catalog.Application.DTOs.PagedResult<T>
 */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

/**
 * Mirrors backend Catalog.Application.DTOs.CategoryDto
 */
export interface Category {
  id: string;
  name: string;
  description: string | null;
  parentCategoryId: string | null;
  slug: string;
  sortOrder: number;
  isActive: boolean;
}

export type ProductStatus = 'Draft' | 'Active' | 'Inactive' | 'Deleted';

/**
 * Mirrors backend Search.API.Models.SearchResult<T>
 */
export interface SearchResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  facets: Record<string, FacetValue[]> | null;
}

export interface FacetValue {
  key: string;
  count: number;
}

/**
 * Query params for the catalog product list endpoint.
 */
export interface ProductListParams {
  page?: number;
  pageSize?: number;
  categoryId?: string;
  sellerId?: string;
  search?: string;
}

/**
 * Query params for the search endpoint.
 */
export interface ProductSearchParams {
  q?: string;
  categoryId?: string;
  priceMin?: number;
  priceMax?: number;
  tags?: string;
  page?: number;
  pageSize?: number;
}
