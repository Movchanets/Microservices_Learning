/**
 * Mirrors backend Catalog.Application.DTOs.SkuDto
 */
export interface Sku {
  id: string;
  skuCode: string;
  price: number;
  currency: string;
  status: string;
  imageUrl: string | null;
  typedAttributes: Record<string, string>;
  flexibleAttributes: Record<string, string>;
  createdAt: string;
}

/**
 * Mirrors backend Catalog.Application.DTOs.ProductDto
 */
export interface Product {
  id: string;
  name: string;
  description: string;
  brand: string | null;
  categoryId: string;
  categoryName: string;
  status: ProductStatus;
  imageUrl: string | null;
  storeId: string;
  tags: string[];
  skus: Sku[];
  gallery: GalleryItem[];
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
  minPrice: number | null;
  maxPrice: number | null;
  currency: string | null;
  skuCount: number;
  defaultSkuId: string | null;
  defaultSkuCode: string | null;
  categoryName: string;
  status: string;
  imageUrl: string | null;
  storeId: string;
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
 * Mirrors backend Catalog.Application.DTOs.VariantAxisDto
 * A single variant axis (e.g., "Color" with values ["Black","White","Blue"]).
 */
export interface VariantAxis {
  key: string;
  displayName: string;
  values: string[];
}

/**
 * Mirrors backend Catalog.Application.DTOs.VariantOptionDto
 * A single combination of variant values and its availability.
 */
export interface VariantOption {
  combination: Record<string, string>;
  skuId: string | null;
  skuCode: string | null;
  price: number | null;
  currency: string | null;
  imageUrl: string | null;
  isAvailable: boolean;
}

/**
 * Mirrors backend Catalog.Application.DTOs.VariantMatrixDto
 * Variant matrix for a product — all possible SKU combinations
 * based on variant-axis attribute definitions.
 */
export interface VariantMatrix {
  productId: string;
  productName: string;
  axes: VariantAxis[];
  options: VariantOption[];
}

/**
 * Gallery item from Media.API
 */
export interface GalleryItem {
  id: string;
  fileName: string;
  contentType: string;
  url: string;
  thumbnailUrl: string | null;
  sizeBytes: number;
  type: 'Image' | 'Video';
  sortOrder: number;
  isPrimary: boolean;
  createdAt: string;
}

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
  storeId?: string;
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
  brand?: string;
  minRating?: number;
  inStock?: boolean;
  page?: number;
  pageSize?: number;
}

/**
 * Mirrors backend Catalog.Application.DTOs.ReviewDto
 */
export interface Review {
  id: string;
  userId: string;
  userName: string;
  rating: number;
  title: string;
  text: string;
  isVerifiedPurchase: boolean;
  photoUrls: string[];
  helpfulCount: number;
  notHelpfulCount: number;
  sellerResponse: string | null;
  sellerResponseDate: string | null;
  createdAt: string;
}

/**
 * Mirrors backend Catalog.Application.DTOs.ReviewSummaryDto
 */
export interface ReviewSummary {
  averageRating: number;
  totalReviews: number;
  ratingDistribution: Partial<Record<number, number>>;
}

/**
 * Request payload for creating a review.
 * userId and userName are derived server-side from auth claims.
 */
export interface CreateReviewRequest {
  rating: number;
  title: string;
  text: string;
  photoUrls?: string[];
}
