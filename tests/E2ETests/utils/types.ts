/**
 * Shared type interfaces for E2E API helpers.
 */

export interface BffUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
}

export interface StoreResult {
  id: string;
  sellerId: string;
  name: string;
  description: string;
  verificationStatus: string;
}

/** SKU detail returned as part of a product. */
export interface SkuResult {
  id: string;
  skuCode: string;
  price: number;
  currency: string;
  status: string;
  typedAttributes?: Record<string, unknown>;
  flexibleAttributes?: Record<string, unknown>;
  createdAt?: string;
}

/** Full product detail (single product response). */
export interface ProductResult {
  id: string;
  name: string;
  description?: string;
  categoryId?: string;
  categoryName?: string;
  status: string;
  imageUrl?: string;
  brand?: string;
  storeId: string;
  tags?: string[];
  skus: SkuResult[];
  createdAt?: string;
  updatedAt?: string;
}

/** Product summary in list/search results. */
export interface ProductListResult {
  id: string;
  name: string;
  minPrice: number;
  maxPrice: number;
  currency: string;
  skuCount: number;
  defaultSkuId?: string;
  defaultSkuCode?: string;
  categoryName?: string;
  status: string;
  imageUrl?: string;
  storeId: string;
  createdAt?: string;
}

export interface CategoryResult {
  id: string;
  name: string;
  slug: string;
}

export interface OrderResult {
  id: string;
  buyerId: string;
  status: number;
  statusName: string;
  totalAmount: number;
  createdAt: string;
  completedAt: string | null;
  items: OrderItemResult[];
}

export interface OrderItemResult {
  id: string;
  productId: string;
  productName: string;
  skuCode: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

/** A single image in a product or SKU gallery. */
export interface GalleryEntry {
  id: string;
  url: string;
  thumbnailUrl?: string;
  isPrimary: boolean;
  fileName: string;
}

export interface InventoryResult {
  skuId: string;
  skuCode: string;
  availableQuantity: number;
  reservedQuantity: number;
}
