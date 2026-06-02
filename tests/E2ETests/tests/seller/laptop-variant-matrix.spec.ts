/**
 * E2E Tests: Laptop Product with RAM × Storage Variant Matrix
 *
 * Tests the full lifecycle of a single laptop product with multiple variant axes:
 *   1. Create a "Laptops" category with 2 variant axes (RAM, storage)
 *   2. Create a single laptop product (e.g., "MacBook Pro M4")
 *   3. Bulk-generate SKU combinations (RAM × Storage Cartesian product)
 *   4. Verify all combinations were created with correct attributes
 *   5. Upload per-SKU images for specific variants
 *   6. Verify per-SKU galleries are independent
 *   7. Exclude specific combinations and verify they're not created
 *   8. Change price on a specific SKU
 *   9. Remove a SKU and verify it's soft-deleted
 *   10. Reject duplicate variant combination
 *
 * Variant matrix: 3 RAM × 4 storage = 12 combinations
 * With exclusions: 12 - 2 = 10 expected SKUs
 */

import { authTest as test, expect } from '../../fixtures/auth.fixture';
import {
  createProduct,
  addSku,
  bulkAddSku,
  getProductById,
  ensureCategoryExists,
  addAttributeDefinition,
  getAttributeDefinitions,
  activateProduct,
} from '../../utils/catalog-helpers';
import { ensureStoreExists } from '../../utils/store-helpers';
import {
  uploadMedia,
  getGallery,
  getTestImagePath,
} from '../../utils/media-helpers';
import type { StoreResult, ProductResult, SkuResult } from '../../utils/types';
import { TIMEOUTS } from '../../utils/constants';

// ── Variant axis definitions for a single laptop model ─────

const VARIANT_AXES = {
  ram: {
    key: 'ram',
    displayName: 'RAM',
    values: ['16GB', '32GB', '64GB'],
  },
  storage: {
    key: 'storage',
    displayName: 'Storage',
    values: ['256GB', '512GB', '1TB', '2TB'],
  },
};

// Total combinations: 3 × 4 = 12
// Excluded: 64GB+256GB (makes no sense), 16GB+2TB (underpowered for 2TB)
const EXCLUDED_COMBINATIONS = [
  'ram:64GB,storage:256GB',
  'ram:16GB,storage:2TB',
];

const EXPECTED_SKU_COUNT = 12 - 2; // 10

// ── Test Suite ─────────────────────────────────────────────

test.describe('Seller: Laptop Variant Matrix (RAM × Storage)', () => {
  let store: StoreResult;
  let categoryId: string;
  const uniqueId = Math.random().toString(36).substring(7).toUpperCase();

  test.beforeAll(async ({ sellerApi, sellerUser, adminApi }) => {
    // 1. Create and verify a store
    store = await ensureStoreExists(
      sellerApi, adminApi, sellerUser.id,
      `Laptop Store ${uniqueId}`, 'E2E laptop variant test store'
    );

    // 2. Create "Laptops" category
    const category = await ensureCategoryExists(adminApi, `Laptops ${uniqueId}`, 'Laptop computers');
    categoryId = category.id;

    // 3. Add variant axis attribute definitions
    //    target=1 (Sku), valueType=2 (Select), isFilterable=true, isVariantAxis=true
    for (const axis of Object.values(VARIANT_AXES)) {
      await addAttributeDefinition(adminApi, categoryId, {
        key: axis.key,
        displayName: axis.displayName,
        target: 1,        // Sku
        valueType: 2,     // Select
        isFilterable: true,
        isRequired: true,
        allowedValues: axis.values,
        isVariantAxis: true,
      });
    }

    // 4. Verify attribute definitions were created
    const attrs = await getAttributeDefinitions(adminApi, categoryId, true);
    const variantAxes = attrs.filter(a => a.isVariantAxis);
    expect(variantAxes).toHaveLength(2);
  });

  // ── Scenario 1: Bulk-generate all SKU combinations ──────

  test('should create laptop with 10 SKU variants via bulk API', async ({ sellerApi }) => {
    let product: ProductResult;

    await test.step('Create laptop product', async () => {
      product = await createProduct(sellerApi, {
        name: `MacBook Pro M4 ${uniqueId}`,
        description: 'Apple MacBook Pro M4 with multiple RAM and storage configurations',
        categoryId,
        storeId: store.id,
        brand: 'Apple',
        tags: ['laptop', 'apple', 'macbook', 'e2e'],
      });
      expect(product).toBeTruthy();
      expect(product.id).toBeTruthy();
    });

    await test.step('Bulk-generate 12 combinations (2 excluded → 10 SKUs)', async () => {
      const variantCombinations: Record<string, string[]> = {};
      for (const axis of Object.values(VARIANT_AXES)) {
        variantCombinations[axis.key] = axis.values;
      }

      const result = await bulkAddSku(sellerApi, product!.id, {
        variantCombinations,
        basePrice: 1999.99,
        currency: 'USD',
        excludedCombinations: EXCLUDED_COMBINATIONS,
        skuCodePrefix: `MBP-M4-${uniqueId}`,
      });

      expect(result.createdCount).toBe(EXPECTED_SKU_COUNT);
      expect(result.totalCombinations).toBe(12);
      expect(result.createdSkus).toHaveLength(EXPECTED_SKU_COUNT);
    });

    await test.step('Verify product has 10 SKUs', async () => {
      const fetched = await getProductById(sellerApi, product!.id);
      expect(fetched).toBeTruthy();
      expect(fetched!.skus).toHaveLength(EXPECTED_SKU_COUNT);
    });

    await test.step('Verify SKU codes follow prefix pattern', async () => {
      const fetched = await getProductById(sellerApi, product!.id);
      for (const sku of fetched!.skus) {
        expect(sku.skuCode).toMatch(new RegExp(`^MBP-M4-${uniqueId}-`));
      }
    });

    await test.step('Verify all SKUs have correct typed attributes', async () => {
      const fetched = await getProductById(sellerApi, product!.id);
      for (const sku of fetched!.skus) {
        expect(sku.typedAttributes).toBeTruthy();
        expect(sku.typedAttributes!.ram).toBeDefined();
        expect(sku.typedAttributes!.storage).toBeDefined();

        // Verify values are from allowed sets
        expect(VARIANT_AXES.ram.values).toContain(sku.typedAttributes!.ram);
        expect(VARIANT_AXES.storage.values).toContain(sku.typedAttributes!.storage);
      }
    });

    await test.step('Verify excluded combinations are NOT present', async () => {
      const fetched = await getProductById(sellerApi, product!.id);
      for (const excluded of EXCLUDED_COMBINATIONS) {
        const parts = excluded.split(',').reduce((acc, p) => {
          const [k, v] = p.split(':');
          acc[k] = v;
          return acc;
        }, {} as Record<string, string>);

        const found = fetched!.skus.find(sku =>
          sku.typedAttributes!.ram === parts.ram &&
          sku.typedAttributes!.storage === parts.storage
        );
        expect(found).toBeUndefined();
      }
    });

    await test.step('Verify all SKUs have the base price', async () => {
      const fetched = await getProductById(sellerApi, product!.id);
      for (const sku of fetched!.skus) {
        expect(sku.price).toBe(1999.99);
        expect(sku.currency).toBe('USD');
      }
    });

    await test.step('Verify each SKU has a unique RAM+storage combination', async () => {
      const fetched = await getProductById(sellerApi, product!.id);
      const signatures = fetched!.skus.map(s =>
        `${s.typedAttributes!.ram}-${s.typedAttributes!.storage}`
      );
      const unique = new Set(signatures);
      expect(unique.size).toBe(fetched!.skus.length);
    });
  });

  // ── Scenario 2: Per-SKU gallery isolation ────────────────

  test('should upload per-SKU images and verify gallery isolation', async ({ sellerApi }) => {
    let product: ProductResult;
    let skus: SkuResult[];

    await test.step('Create laptop with 3 RAM+storage configs', async () => {
      product = await createProduct(sellerApi, {
        name: `Gallery Laptop ${uniqueId}`,
        description: 'MacBook Pro for testing per-SKU gallery isolation',
        categoryId,
        storeId: store.id,
        brand: 'Apple',
      });

      const configurations = [
        { ram: '16GB', storage: '256GB' },
        { ram: '32GB', storage: '512GB' },
        { ram: '64GB', storage: '1TB' },
      ];

      skus = [];
      for (const config of configurations) {
        const sku = await addSku(sellerApi, product!.id, {
          skuCode: `GL-${config.ram}-${config.storage}-${uniqueId}`,
          price: 1999.99,
          currency: 'USD',
          typedAttributes: config,
        });
        skus.push(sku);
      }
    });

    await test.step('Upload 2 images to SKU 0 (16GB/256GB)', async () => {
      await uploadMedia(sellerApi, getTestImagePath('product-1.jpg'), skus[0].id, 'SKU', true);
      await uploadMedia(sellerApi, getTestImagePath('product-2.jpg'), skus[0].id, 'SKU', false);
    });

    await test.step('Upload 1 image to SKU 1 (32GB/512GB)', async () => {
      await uploadMedia(sellerApi, getTestImagePath('product-3.jpg'), skus[1].id, 'SKU', true);
    });

    await test.step('Upload 3 images to SKU 2 (64GB/1TB)', async () => {
      await uploadMedia(sellerApi, getTestImagePath('sku-red-1.jpg'), skus[2].id, 'SKU', true);
      await uploadMedia(sellerApi, getTestImagePath('sku-blue-1.jpg'), skus[2].id, 'SKU', false);
      await uploadMedia(sellerApi, getTestImagePath('product-1.jpg'), skus[2].id, 'SKU', false);
    });

    await test.step('Verify SKU 0 gallery has 2 images', async () => {
      const gallery = await getGallery(sellerApi, 'SKU', skus[0].id);
      expect(gallery).toHaveLength(2);
      const primary = gallery.find(img => img.isPrimary === true);
      expect(primary).toBeTruthy();
    });

    await test.step('Verify SKU 1 gallery has 1 image', async () => {
      const gallery = await getGallery(sellerApi, 'SKU', skus[1].id);
      expect(gallery).toHaveLength(1);
      expect(gallery[0].isPrimary).toBe(true);
    });

    await test.step('Verify SKU 2 gallery has 3 images', async () => {
      const gallery = await getGallery(sellerApi, 'SKU', skus[2].id);
      expect(gallery).toHaveLength(3);
      const primary = gallery.find(img => img.isPrimary === true);
      expect(primary!.fileName).toContain('sku-red-1');
    });

    await test.step('Verify galleries are independent', async () => {
      const gallery0 = await getGallery(sellerApi, 'SKU', skus[0].id);
      const gallery2 = await getGallery(sellerApi, 'SKU', skus[2].id);

      const sku0Files = gallery0.map(g => g.fileName).sort();
      const sku2Files = gallery2.map(g => g.fileName).sort();
      expect(sku0Files).not.toEqual(sku2Files);
    });
  });

  // ── Scenario 3: Change price on specific SKU ─────────────

  test('should change price on a specific RAM+storage variant', async ({ sellerApi }) => {
    let product: ProductResult;
    let targetSku: SkuResult;

    await test.step('Create laptop with 3 configs', async () => {
      product = await createProduct(sellerApi, {
        name: `Price Test MacBook ${uniqueId}`,
        description: 'MacBook Pro for testing per-SKU pricing',
        categoryId,
        storeId: store.id,
        brand: 'Apple',
      });

      await addSku(sellerApi, product!.id, {
        skuCode: `PT-16-256-${uniqueId}`,
        price: 1999.99,
        currency: 'USD',
        typedAttributes: { ram: '16GB', storage: '256GB' },
      });

      targetSku = await addSku(sellerApi, product!.id, {
        skuCode: `PT-32-512-${uniqueId}`,
        price: 2499.99,
        currency: 'USD',
        typedAttributes: { ram: '32GB', storage: '512GB' },
      });

      await addSku(sellerApi, product!.id, {
        skuCode: `PT-64-1TB-${uniqueId}`,
        price: 3499.99,
        currency: 'USD',
        typedAttributes: { ram: '64GB', storage: '1TB' },
      });
    });

    await test.step('Change price on the 32GB/512GB SKU', async () => {
      const response = await sellerApi.patch(
        `/api/catalog/products/${product!.id}/skus/${targetSku.id}/price`,
        { data: { price: 2699.99, currency: 'USD' } }
      );
      expect(response.ok()).toBeTruthy();
    });

    await test.step('Verify price changed on target SKU only', async () => {
      const fetched = await getProductById(sellerApi, product!.id);
      expect(fetched).toBeTruthy();

      const sku16 = fetched!.skus.find(s => s.skuCode === `PT-16-256-${uniqueId}`);
      const sku32 = fetched!.skus.find(s => s.skuCode === `PT-32-512-${uniqueId}`);
      const sku64 = fetched!.skus.find(s => s.skuCode === `PT-64-1TB-${uniqueId}`);

      expect(sku16!.price).toBe(1999.99);   // unchanged
      expect(sku32!.price).toBe(2699.99);   // changed
      expect(sku64!.price).toBe(3499.99);   // unchanged
    });
  });

  // ── Scenario 4: Remove a SKU (soft-delete) ───────────────

  test('should soft-delete a SKU and verify it disappears', async ({ sellerApi }) => {
    let product: ProductResult;
    let skuToRemove: SkuResult;

    await test.step('Create laptop with 2 configs', async () => {
      product = await createProduct(sellerApi, {
        name: `Remove SKU MacBook ${uniqueId}`,
        description: 'MacBook Pro for testing SKU removal',
        categoryId,
        storeId: store.id,
        brand: 'Apple',
      });

      skuToRemove = await addSku(sellerApi, product!.id, {
        skuCode: `RM-REMOVE-${uniqueId}`,
        price: 1999.99,
        currency: 'USD',
        typedAttributes: { ram: '16GB', storage: '256GB' },
      });

      await addSku(sellerApi, product!.id, {
        skuCode: `RM-KEEP-${uniqueId}`,
        price: 2999.99,
        currency: 'USD',
        typedAttributes: { ram: '64GB', storage: '1TB' },
      });
    });

    await test.step('Verify product has 2 SKUs', async () => {
      const fetched = await getProductById(sellerApi, product!.id);
      expect(fetched!.skus).toHaveLength(2);
    });

    await test.step('Remove the 16GB/256GB SKU', async () => {
      const response = await sellerApi.delete(
        `/api/catalog/products/${product!.id}/skus/${skuToRemove.id}`
      );
      expect(response.ok()).toBeTruthy();
    });

    await test.step('Verify product now has 1 SKU', async () => {
      const fetched = await getProductById(sellerApi, product!.id);
      expect(fetched!.skus).toHaveLength(1);
      expect(fetched!.skus[0].skuCode).toBe(`RM-KEEP-${uniqueId}`);
    });
  });

  // ── Scenario 5: Variant matrix uniqueness validation ─────

  test('should reject duplicate RAM+storage combination', async ({ sellerApi }) => {
    let product: ProductResult;

    await test.step('Create laptop with one SKU (16GB/256GB)', async () => {
      product = await createProduct(sellerApi, {
        name: `Uniqueness Test ${uniqueId}`,
        description: 'MacBook Pro for testing variant uniqueness',
        categoryId,
        storeId: store.id,
        brand: 'Apple',
      });

      await addSku(sellerApi, product!.id, {
        skuCode: `UNIQ-EXISTING-${uniqueId}`,
        price: 1999.99,
        currency: 'USD',
        typedAttributes: { ram: '16GB', storage: '256GB' },
      });
    });

    await test.step('Try to add SKU with same RAM+storage — expect 400', async () => {
      const response = await sellerApi.post(`/api/catalog/products/${product!.id}/skus`, {
        data: {
          skuCode: `UNIQ-DUPLICATE-${uniqueId}`,
          price: 2099.99,
          currency: 'USD',
          typedAttributes: { ram: '16GB', storage: '256GB' },
        },
      });
      // Backend should reject with 400 due to variant-axis uniqueness
      expect(response.status()).toBe(400);
    });

    await test.step('Add SKU with different combination (32GB/512GB) — expect success', async () => {
      const response = await sellerApi.post(`/api/catalog/products/${product!.id}/skus`, {
        data: {
          skuCode: `UNIQ-UNIQUE-${uniqueId}`,
          price: 2499.99,
          currency: 'USD',
          typedAttributes: { ram: '32GB', storage: '512GB' },
        },
      });
      expect(response.ok()).toBeTruthy();
    });

    await test.step('Verify product has 2 SKUs', async () => {
      const fetched = await getProductById(sellerApi, product!.id);
      expect(fetched!.skus).toHaveLength(2);
    });
  });

  // ── Scenario 6: Activate product with SKUs ───────────────

  test('should activate laptop after adding SKUs', async ({ sellerApi }) => {
    let product: ProductResult;

    await test.step('Create laptop (starts as Draft)', async () => {
      product = await createProduct(sellerApi, {
        name: `Activation MacBook ${uniqueId}`,
        description: 'MacBook Pro for testing activation flow',
        categoryId,
        storeId: store.id,
        brand: 'Apple',
      });
      expect(product!.status).toBe('Draft');
    });

    await test.step('Add a SKU', async () => {
      await addSku(sellerApi, product!.id, {
        skuCode: `ACT-SKU-${uniqueId}`,
        price: 1999.99,
        currency: 'USD',
        typedAttributes: { ram: '16GB', storage: '256GB' },
      });
    });

    await test.step('Activate the product', async () => {
      await activateProduct(sellerApi, product!.id);
    });

    await test.step('Verify product is Active', async () => {
      const fetched = await getProductById(sellerApi, product!.id);
      expect(fetched!.status).toBe('Active');
    });
  });
});
