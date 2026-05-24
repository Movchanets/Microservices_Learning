/**
 * Catalog API helpers — products, categories, and inventory.
 */

import { APIRequestContext } from '@playwright/test';
import type { ProductResult, CategoryResult, InventoryResult } from './types';

// ── Products ──

export async function createProduct(
  api: APIRequestContext,
  product: {
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
): Promise<ProductResult> {
  const response = await api.post('/api/catalog/products', {
    data: product,
  });
  if (!response.ok()) {
    throw new Error(`Create product failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function getProductBySku(
  api: APIRequestContext,
  sku: string
): Promise<ProductResult | null> {
  const response = await api.get(`/api/catalog/products/sku/${sku}`);
  if (response.status() === 404) return null;
  if (!response.ok()) {
    throw new Error(`Get product by SKU failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function activateProduct(
  api: APIRequestContext,
  productId: string
): Promise<void> {
  const response = await api.put(`/api/catalog/products/${productId}/activate`);
  // 409 = already active, ignore
  if (!response.ok() && response.status() !== 409) {
    throw new Error(`Activate product failed: ${response.status()} ${await response.text()}`);
  }
}

// ── Categories ──

export async function getCategories(
  api: APIRequestContext
): Promise<CategoryResult[]> {
  const response = await api.get('/api/catalog/categories');
  if (!response.ok()) {
    throw new Error(`Get categories failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function createCategory(
  api: APIRequestContext,
  name: string,
  description: string
): Promise<CategoryResult> {
  const response = await api.post('/api/catalog/categories', {
    data: { name, description },
  });
  if (!response.ok()) {
    throw new Error(`Create category failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

// ── Inventory ──

export async function createInventoryItem(
  api: APIRequestContext,
  sku: string,
  initialQuantity: number,
  storeId?: string,
  productId?: string
): Promise<void> {
  const response = await api.post('/api/inventory/items', {
    data: { sku, initialQuantity, storeId, productId },
  });
  if (!response.ok()) {
    // Ignore 409 Conflict — item may already exist
    if (response.status() === 409) return;
    throw new Error(`Create inventory item failed: ${response.status()} ${await response.text()}`);
  }
}

export async function setInventoryStock(
  api: APIRequestContext,
  sku: string,
  quantity: number,
  storeId: string,
  productId: string
): Promise<void> {
  const response = await api.put(`/api/inventory/items/${sku}/stock`, {
    data: { quantity, storeId, productId },
  });
  if (!response.ok()) {
    throw new Error(`Set inventory stock failed: ${response.status()} ${await response.text()}`);
  }
}

export async function getInventoryItem(
  api: APIRequestContext,
  sku: string
): Promise<InventoryResult | null> {
  const response = await api.get(`/api/inventory/items/${sku}`);
  if (response.status() === 404) return null;
  if (!response.ok()) {
    throw new Error(`Get inventory failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

// ── Idempotent Helpers ──

/**
 * Ensures a category exists. Creates if not present. Returns the category.
 */
export async function ensureCategoryExists(
  adminApi: APIRequestContext,
  name: string,
  description: string
): Promise<CategoryResult> {
  const existing = await getCategories(adminApi);
  const match = existing.find(c => c.name === name);
  if (match) return match;

  return createCategory(adminApi, name, description);
}

/**
 * Ensures a product exists with inventory. Creates product + inventory if not present.
 */
export async function ensureProductExists(
  sellerApi: APIRequestContext,
  product: {
    name: string;
    description: string;
    sku: string;
    price: number;
    currency: string;
    categoryId: string;
    storeId: string;
    tags?: string[];
    imageUrl?: string;
  },
  initialStock: number
): Promise<ProductResult> {
  // Check if product already exists by SKU
  const existing = await getProductBySku(sellerApi, product.sku);
  if (existing) {
    return existing;
  }

  // Create product
  const created = await createProduct(sellerApi, product);

  // Activate the product (ignore if endpoint doesn't exist)
  try {
    await activateProduct(sellerApi, created.id);
  } catch {
    // Activation endpoint may not exist
  }

  // Set inventory stock
  if (initialStock > 0) {
    await setInventoryStock(sellerApi, product.sku, initialStock, created.storeId, created.id);
  }

  return created;
}
