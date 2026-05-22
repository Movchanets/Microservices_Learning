// Seller Dashboard data models.
// Defines types for product management, store settings, and sales metrics.

export interface SellerProduct {
  id: string;
  name: string;
  description: string;
  sku: string;
  price: number;
  currency: string;
  categoryId: string;
  categoryName: string;
  status: 'Draft' | 'Active' | 'Inactive' | 'Deleted';
  imageUrl: string | null;
  storeId: string;
  tags: string[];
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateProductRequest {
  name: string;
  description: string;
  sku: string;
  price: number;
  currency: string;
  categoryId: string;
  storeId: string;
  tags?: string[];
  imageUrl?: string;
}

export interface UpdateProductRequest {
  name?: string;
  description?: string;
  price?: number;
  categoryId?: string;
  stockQuantity?: number;
  imageUrl?: string;
  isActive?: boolean;
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
