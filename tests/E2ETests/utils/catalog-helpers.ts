/**
 * Catalog API helpers — products, categories, and inventory.
 */

import { APIRequestContext } from '@playwright/test';
import type { ProductResult, SkuResult, CategoryResult, InventoryResult } from './types';

// ── Products ──

export async function createProduct(
  api: APIRequestContext,
  product: {
    name: string;
    description: string;
    categoryId: string;
    storeId: string;
    brand?: string;
    tags?: string[];
    imageUrl?: string;
    variantAxisIds?: string[];
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

export async function addSku(
  api: APIRequestContext,
  productId: string,
  sku: {
    skuCode: string;
    price: number;
    currency: string;
    typedAttributes?: Record<string, unknown>;
    flexibleAttributes?: Record<string, unknown>;
  }
): Promise<SkuResult> {
  const response = await api.post(`/api/catalog/products/${productId}/skus`, {
    data: sku,
  });
  if (!response.ok()) {
    throw new Error(`Add SKU failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function bulkAddSku(
  api: APIRequestContext,
  productId: string,
  request: {
    variantCombinations: Record<string, string[]>;
    basePrice?: number;
    currency?: string;
    excludedCombinations?: string[];
    skuCodePrefix?: string;
  }
): Promise<{ createdCount: number; totalCombinations: number; createdSkus: SkuResult[]; errors?: string[] }> {
  const response = await api.post(`/api/catalog/products/${productId}/skus/bulk`, {
    data: request,
  });
  if (!response.ok()) {
    throw new Error(`Bulk add SKU failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

export async function getProductById(
  api: APIRequestContext,
  productId: string
): Promise<ProductResult | null> {
  const response = await api.get(`/api/catalog/products/${productId}`);
  if (response.status() === 404) return null;
  if (!response.ok()) {
    throw new Error(`Get product failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

/**
 * Finds a product that has a SKU matching the given skuCode.
 * Searches via the catalog list endpoint and filters client-side.
 */
export async function getProductBySku(
  api: APIRequestContext,
  skuCode: string
): Promise<ProductResult | null> {
  const response = await api.get('/api/catalog/products');
  if (!response.ok()) return null;
  const data = await response.json();
  // Endpoint returns PagedResult<ProductResult> with { items, totalCount, ... }
  const products: ProductResult[] = data.items ?? data;
  return products.find(p => p.skus?.some(s => s.skuCode === skuCode)) ?? null;
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

/**
 * Idempotently sets inventory stock for a SKU.
 *
 * Uses PUT /api/inventory/items/{skuCode}/stock which creates the item
 * with the given quantity if it doesn't exist, or updates it if it does.
 * This avoids the race condition with MassTransit's SkuCreatedConsumer
 * which auto-creates inventory items with qty=0.
 */
export async function createInventoryItem(
  api: APIRequestContext,
  params: {
    skuCode: string;
    productId: string;
    initialQuantity: number;
    storeId: string;
  }
): Promise<void> {
  await setInventoryStock(api, params.skuCode, params.initialQuantity, params.storeId, params.productId);
}

export async function setInventoryStock(
  api: APIRequestContext,
  skuCode: string,
  quantity: number,
  storeId: string,
  productId: string
): Promise<void> {
  const response = await api.put(`/api/inventory/items/${skuCode}/stock`, {
    data: { quantity, storeId, productId },
  });
  if (!response.ok()) {
    throw new Error(`Set inventory stock failed: ${response.status()} ${await response.text()}`);
  }
}

export async function getInventoryItem(
  api: APIRequestContext,
  skuCode: string
): Promise<InventoryResult | null> {
  const response = await api.get(`/api/inventory/items/${skuCode}`);
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
 * Creates an attribute definition on a category.
 */
export async function addAttributeDefinition(
  api: APIRequestContext,
  categoryId: string,
  attr: {
    key: string;
    displayName: string;
    target: number;       // 0=Product, 1=Sku
    valueType: number;    // 0=Text, 1=Number, 2=Select
    isFilterable: boolean;
    isRequired: boolean;
    sortOrder?: number;
    allowedValues?: string[];
    isVariantAxis?: boolean;
  }
): Promise<{ id: string; key: string; displayName: string }> {
  const response = await api.post(`/api/catalog/categories/${categoryId}/attributes`, {
    data: attr,
  });
  if (!response.ok()) {
    // If attribute already exists (400 or 409), fetch and return the existing one
    if (response.status() === 400 || response.status() === 409) {
      const body = await response.text();
      if (body.includes('already exists')) {
        const existing = await getAttributeDefinitions(api, categoryId);
        const match = existing.find(a => a.key === attr.key);
        if (match) return { id: match.id, key: match.key, displayName: match.displayName };
      }
    }
    throw new Error(`Add attribute definition failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

/**
 * Gets attribute definitions for a category.
 */
export async function getAttributeDefinitions(
  api: APIRequestContext,
  categoryId: string,
  includeInherited = false
): Promise<Array<{ id: string; key: string; displayName: string; target: string; valueType: string; allowedValues: string[] }>> {
  const params = includeInherited ? '?includeInherited=true' : '';
  const response = await api.get(`/api/catalog/categories/${categoryId}/attributes${params}`);
  if (!response.ok()) {
    throw new Error(`Get attribute definitions failed: ${response.status()} ${await response.text()}`);
  }
  return response.json();
}

/**
 * Ensures a product exists with at least one SKU and inventory.
 * Creates product → adds SKU → activates → sets inventory if not present.
 */
export async function ensureProductExists(
  sellerApi: APIRequestContext,
  product: {
    name: string;
    description: string;
    categoryId: string;
    storeId: string;
    brand?: string;
    tags?: string[];
    imageUrl?: string;
  },
  sku: {
    skuCode: string;
    price: number;
    currency: string;
  },
  initialStock: number
): Promise<ProductResult> {
  // Check if product already exists by SKU code
  const existing = await getProductBySku(sellerApi, sku.skuCode);
  if (existing) {
    return existing;
  }

  // Create product (no SKU/price — just product metadata)
  const created = await createProduct(sellerApi, product);

  // Add SKU to the product
  const skuResult = await addSku(sellerApi, created.id, sku);

  // Activate the product (ignore if endpoint doesn't exist)
  try {
    await activateProduct(sellerApi, created.id);
  } catch {
    // Activation endpoint may not exist
  }

  // Set inventory stock
  if (initialStock > 0) {
    await createInventoryItem(sellerApi, {
      skuCode: skuResult.skuCode,
      productId: created.id,
      initialQuantity: initialStock,
      storeId: created.storeId,
    });
  }

  // Re-fetch the product so the returned object has the SKU populated
  const full = await getProductById(sellerApi, created.id);
  return full ?? { ...created, skus: [skuResult] };
}
