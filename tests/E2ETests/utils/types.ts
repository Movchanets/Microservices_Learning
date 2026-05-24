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

export interface ProductResult {
  id: string;
  name: string;
  sku: string;
  price: number;
  storeId: string;
  status: string;
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
  sku: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

export interface InventoryResult {
  sku: string;
  availableQuantity: number;
}
