// Seller Dashboard data models.
// Defines types for product management, store settings, and sales metrics.

import { Sku } from '../catalog/catalog.models';

export interface SellerProduct {
  id: string;
  name: string;
  description: string;
  brand: string | null;
  categoryId: string;
  categoryName: string;
  status: 'Draft' | 'Active' | 'Inactive' | 'Deleted';
  imageUrl: string | null;
  storeId: string;
  tags: string[];
  skus: Sku[];
  skuCount?: number;
  minPrice?: number;
  maxPrice?: number;
  currency?: string;
  defaultSkuId?: string;
  defaultSkuCode?: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateProductRequest {
  name: string;
  description: string;
  brand?: string;
  categoryId: string;
  storeId: string;
  tags?: string[];
  imageUrl?: string;
}

export interface AddSkuRequest {
  skuCode: string;
  price: number;
  currency: string;
  typedAttributes?: Record<string, string>;
  flexibleAttributes?: Record<string, string>;
}

export interface UpdateProductRequest {
  name: string;
  description: string;
  categoryId: string;
  brand?: string;
  tags?: string[];
  imageUrl?: string;
}

export interface BulkAddSkuRequest {
  variantCombinations: Record<string, string[]>;
  basePrice?: number;
  currency?: string;
  excludedCombinations?: string[];
  skuCodePrefix?: string;
}

export interface BulkAddSkuResult {
  createdCount: number;
  totalCombinations: number;
  createdSkus: Sku[];
  errors?: string[];
}

export interface StoreSettings {
  storeId: string;
  storeName: string;
  description: string;
  logoUrl: string | null;
  contactEmail: string;
  isActive: boolean;
  verificationStatus?: 'Pending' | 'Verified' | 'Rejected';
  rejectionReason?: string | null;
  createdAt?: string;
  verifiedAt?: string | null;
}

export interface SalesSummary {
  totalOrders: number;
  totalRevenue: number;
  pendingOrders: number;
  completedOrders: number;
}
