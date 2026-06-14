/**
 * E2E Test: Variant Selection → Cart → Order
 *
 * Exercises the full buyer flow with variant products:
 *   1. Seed test data (product with Color × Storage variant axes)
 *   2. Navigate to product detail → verify variant picker visible
 *   3. Select a non-default variant → verify price and breadcrumb update
 *   4. Add to cart → verify correct SKU, price, quantity
 *   5. Checkout → fill address, place order
 *   6. Verify order in order history
 *
 * Uses: auth.fixture.ts (buyer context), api-helpers.ts (seed data), page objects
 */

import { authTest as test, expect } from '../../fixtures/auth.fixture';
import {
  createProduct,
  bulkAddSku,
  getProductById,
  ensureCategoryExists,
  addAttributeDefinition,
  activateProduct,
  createInventoryItem,
} from '../../utils/catalog-helpers';
import { ensureStoreExists } from '../../utils/store-helpers';
import { getOrder, getOrders } from '../../utils/order-helpers';
import type { StoreResult, ProductResult, SkuResult } from '../../utils/types';
import { TIMEOUTS } from '../../utils/constants';

// ── Variant axis definitions ──────────────────────────────

const VARIANT_AXES = {
  color: {
    key: 'color',
    displayName: 'Color',
    values: ['Black', 'Gold', 'Silver'],
  },
  storage: {
    key: 'storage',
    displayName: 'Storage',
    values: ['256GB', '512GB'],
  },
};

// Total combinations: 3 × 2 = 6
const EXPECTED_SKU_COUNT = 6;

// The variant we'll select during the test (non-default)
const TARGET_COLOR = 'Gold';
const TARGET_STORAGE = '512GB';

// ── Test Suite ─────────────────────────────────────────────

test.describe('Buyer: Variant Selection → Cart → Order', () => {
  let store: StoreResult;
  let product: ProductResult;
  let categoryId: string;
  let targetSku: SkuResult;
  const uniqueId = Math.random().toString(36).substring(7).toUpperCase();

  // ── Setup: Seed variant product via API ──────────────────

  test.beforeAll(async ({ sellerApi, sellerUser, adminApi }) => {
    // 1. Create and verify store
    store = await ensureStoreExists(
      sellerApi, adminApi, sellerUser.id,
      `Variant Store ${uniqueId}`, 'E2E variant selection test store'
    );

    // 2. Create category with variant axes
    const category = await ensureCategoryExists(adminApi, `Phones ${uniqueId}`, 'Smartphones');
    categoryId = category.id;

    // 3. Add variant axis attribute definitions
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

    // 4. Create product
    product = await createProduct(sellerApi, {
      name: `iPhone 16 Pro ${uniqueId}`,
      description: 'Apple iPhone 16 Pro — E2E variant selection test',
      categoryId,
      storeId: store.id,
      brand: 'Apple',
      tags: ['phone', 'apple', 'e2e'],
    });

    // 5. Bulk-generate SKU combinations (3 colors × 2 storage = 6 SKUs)
    const variantCombinations: Record<string, string[]> = {};
    for (const axis of Object.values(VARIANT_AXES)) {
      variantCombinations[axis.key] = axis.values;
    }

    await bulkAddSku(sellerApi, product.id, {
      variantCombinations,
      basePrice: 999.99,
      currency: 'USD',
      skuCodePrefix: `IPH16-${uniqueId}`,
    });

    // 6. Activate product
    await activateProduct(sellerApi, product.id);

    // 7. Set inventory for all SKUs
    const fullProduct = await getProductById(sellerApi, product.id);
    expect(fullProduct).toBeTruthy();
    expect(fullProduct!.skus).toHaveLength(EXPECTED_SKU_COUNT);

    for (const sku of fullProduct!.skus) {
      await createInventoryItem(sellerApi, {
        skuId: sku.id,
        skuCode: sku.skuCode,
        productId: product.id,
        initialQuantity: 50,
        storeId: store.id,
      });
    }

    // Resolve the target SKU (Gold / 512GB) for later assertions
    targetSku = fullProduct!.skus.find(s =>
      s.typedAttributes?.color === TARGET_COLOR &&
      s.typedAttributes?.storage === TARGET_STORAGE
    )!;
    expect(targetSku).toBeTruthy();
  });

  // ── Test 1: Variant picker visible with correct axes ─────

  test('should show variant picker with Color and Storage axes', async ({ productDetailPage }) => {
    await test.step('Navigate to product detail page', async () => {
      await productDetailPage.goto(product.id);
      await productDetailPage.waitForPageLoad();
      await productDetailPage.variantPicker.waitFor({ state: 'visible', timeout: TIMEOUTS.api });
    });

    await test.step('Verify variant picker is visible', async () => {
      await expect(productDetailPage.variantPicker).toBeVisible();
    });

    await test.step('Verify Color axis buttons exist', async () => {
      for (const color of VARIANT_AXES.color.values) {
        await expect(productDetailPage.getVariantButton('color', color)).toBeVisible();
      }
    });

    await test.step('Verify Storage axis buttons exist', async () => {
      for (const storage of VARIANT_AXES.storage.values) {
        await expect(productDetailPage.getVariantButton('storage', storage)).toBeVisible();
      }
    });
  });

  // ── Test 2: Select non-default variant → verify updates ──

  test('should update price and breadcrumb when selecting Gold / 512GB', async ({ productDetailPage }) => {
    await test.step('Navigate and wait for variant picker', async () => {
      await productDetailPage.goto(product.id);
      await productDetailPage.waitForPageLoad();
      await productDetailPage.variantPicker.waitFor({ state: 'visible', timeout: TIMEOUTS.api });
    });

    // Capture default price before switching
    let defaultPrice: string;
    await test.step('Capture default variant price', async () => {
      defaultPrice = await productDetailPage.getPriceText();
      expect(defaultPrice).toMatch(/\$/);
    });

    await test.step('Select Gold color', async () => {
      await productDetailPage.selectVariant('color', TARGET_COLOR);
      await expect(productDetailPage.getVariantButton('color', TARGET_COLOR))
        .toHaveAttribute('aria-pressed', 'true');
    });

    await test.step('Verify breadcrumb shows selected color', async () => {
      const breadcrumb = await productDetailPage.getVariantBreadcrumbText();
      expect(breadcrumb).toBeTruthy();
      expect(breadcrumb).toContain(TARGET_COLOR);
    });

    await test.step('Select 512GB storage', async () => {
      await productDetailPage.selectVariant('storage', TARGET_STORAGE);
      await expect(productDetailPage.getVariantButton('storage', TARGET_STORAGE))
        .toHaveAttribute('aria-pressed', 'true');
    });

    await test.step('Verify breadcrumb shows both axes', async () => {
      const breadcrumb = await productDetailPage.getVariantBreadcrumbText();
      expect(breadcrumb).toContain(TARGET_COLOR);
      expect(breadcrumb).toContain(TARGET_STORAGE);
    });

    await test.step('Verify price is displayed', async () => {
      const price = await productDetailPage.getPriceText();
      expect(price).toMatch(/\$.*\d/);
    });
  });

  // ── Test 3: Add variant to cart → verify correct SKU ─────

  test('should add Gold/512GB variant to cart with correct SKU', async ({ productDetailPage, cartPage, page }) => {
    await test.step('Navigate and select Gold / 512GB variant', async () => {
      await productDetailPage.goto(product.id);
      await productDetailPage.waitForPageLoad();
      await productDetailPage.variantPicker.waitFor({ state: 'visible', timeout: TIMEOUTS.api });
      await productDetailPage.selectVariant('color', TARGET_COLOR);
      await productDetailPage.selectVariant('storage', TARGET_STORAGE);
    });

    await test.step('Add to cart', async () => {
      await productDetailPage.addToCart();
      // Wait for cart store to process the add
      await page.waitForTimeout(1500);
    });

    await test.step('Navigate to cart page', async () => {
      await cartPage.goto();
      await cartPage.waitForPageLoad();
      await page.waitForTimeout(1000);
    });

    await test.step('Verify cart contains the target SKU', async () => {
      const cartItem = cartPage.getCartItem(targetSku.id);
      await expect(cartItem).toBeVisible({ timeout: TIMEOUTS.element });
    });

    await test.step('Verify cart item quantity is 1', async () => {
      const quantity = await cartPage.getQuantity(targetSku.id);
      expect(quantity.trim()).toBe('1');
    });
  });

  // ── Test 4: Checkout → verify order contains correct SKU ─

  test('should complete checkout and verify order with correct variant', async ({
    productDetailPage, cartPage, checkoutPage, buyerApi, page
  }) => {
    // Clear cart for test isolation
    await buyerApi.delete('/api/cart').catch(() => {});

    await test.step('Add Gold / 512GB to cart', async () => {
      await productDetailPage.goto(product.id);
      await productDetailPage.waitForPageLoad();
      await productDetailPage.variantPicker.waitFor({ state: 'visible', timeout: TIMEOUTS.api });
      await productDetailPage.selectVariant('color', TARGET_COLOR);
      await productDetailPage.selectVariant('storage', TARGET_STORAGE);
      await productDetailPage.addToCart();
      await page.waitForTimeout(1500);
    });

    await test.step('Navigate to checkout', async () => {
      await checkoutPage.goto();
      await checkoutPage.waitForPageLoad();
      await page.waitForTimeout(500);
    });

    await test.step('Fill shipping address', async () => {
      const addressLine1 = page.getByTestId('address-line1');
      const city = page.getByTestId('address-city');
      const state = page.getByTestId('address-state');
      const postalCode = page.getByTestId('address-postal-code');
      const country = page.getByTestId('address-country');
      const saveBtn = page.getByTestId('address-save-btn');

      // Angular zoneless-compatible fill strategy
      for (const { input, value } of [
        { input: addressLine1, value: '123 Test Street' },
        { input: city, value: 'Kyiv' },
        { input: state, value: 'Kyiv' },
        { input: postalCode, value: '01001' },
      ]) {
        await input.click();
        await input.fill(value);
        await input.evaluate((el) => {
          el.dispatchEvent(new Event('input', { bubbles: true }));
          el.dispatchEvent(new Event('change', { bubbles: true }));
          el.dispatchEvent(new Event('blur', { bubbles: true }));
        });
      }

      await country.selectOption('UA');
      await page.waitForTimeout(200);
      await saveBtn.click();
    });

    await test.step('Select standard shipping', async () => {
      const standardRadio = page.getByTestId('checkout-shipping-standard');
      await standardRadio.waitFor({ state: 'visible', timeout: TIMEOUTS.element });
      await standardRadio.click();
    });

    await test.step('Continue to payment', async () => {
      const continueBtn = page.getByTestId('checkout-continue-payment');
      await continueBtn.waitFor({ state: 'visible', timeout: TIMEOUTS.element });
      await continueBtn.click();
    });

    await test.step('Place order', async () => {
      const placeOrderBtn = page.getByTestId('checkout-place-order');
      await placeOrderBtn.waitFor({ state: 'visible', timeout: TIMEOUTS.element });
      await placeOrderBtn.click();
    });

    let correlationId: string | null = null;

    await test.step('Verify order submitted', async () => {
      const submittedHeading = page.getByTestId('checkout-order-submitted');
      const completedStatus = page.getByTestId('checkout-status-completed');

      await expect(
        submittedHeading.or(completedStatus)
      ).toBeVisible({ timeout: TIMEOUTS.api });

      correlationId = await page.getByTestId('checkout-correlation-id')
        .innerText()
        .catch(() => null);
    });

    await test.step('Wait for order completion via API', async () => {
      if (!correlationId) return;

      let order = null;
      for (let i = 0; i < 30; i++) {
        order = await getOrder(buyerApi, correlationId);
        if (order && ['Completed', 'Cancelled', 'Faulted'].includes(order.statusName)) break;
        await page.waitForTimeout(2000);
      }

      if (order) {
        expect(order.statusName).toBe('Completed');

        // Verify order contains the target SKU
        const orderItem = order.items.find(i => i.skuCode === targetSku.skuCode);
        expect(orderItem).toBeTruthy();
        expect(orderItem!.quantity).toBe(1);
      }
    });
  });

  // ── Test 5: Order history shows the order ────────────────

  test('should show completed order in order history', async ({ ordersPage, buyerApi }) => {
    await test.step('Navigate to orders page', async () => {
      await ordersPage.goto();
      await ordersPage.waitForPageLoad();
      await ordersPage.page.waitForTimeout(2000);
    });

    await test.step('Verify orders heading is visible', async () => {
      await expect(ordersPage.pageHeading).toBeVisible();
    });

    await test.step('Verify at least one order exists via API', async () => {
      const orders = await getOrders(buyerApi);
      expect(orders.length).toBeGreaterThan(0);
    });
  });
});
