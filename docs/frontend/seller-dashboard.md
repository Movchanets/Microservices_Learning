# Seller Dashboard Feature

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/features/seller-dashboard/` |
| **Stores** | `SellerProductStore`, `StoreSettingsStore`, `InventoryStore` — all `providedIn: 'root'` |
| **Route Prefix** | `/seller` |
| **Guard** | `authGuard` + `roleGuard('Seller', 'Admin')` (on parent route in `app.routes.ts`) |
| **Render Mode** | `RenderMode.Server` (SSR) |
| **Last Updated** | 2026-06-19 |

## Component Structure

```
seller-dashboard/
├── seller-product.store.ts       # SellerProductStore (root singleton)
├── seller-product.service.ts     # HTTP service → Product API
├── seller-product.service.spec.ts # ✅ Tests
├── seller-product.store.spec.ts  # ✅ Tests
├── store-settings.store.ts       # StoreSettingsStore (root singleton)
├── store.service.ts              # HTTP service → StoreManagement API
├── inventory.store.ts            # InventoryStore (root singleton)
├── inventory.store.spec.ts       # ✅ Tests
├── inventory.service.ts          # HTTP service → Inventory API
├── inventory.service.spec.ts     # ✅ Tests
├── seller.models.ts              # SellerProduct, StoreSettings, SalesSummary, etc.
├── seller.routes.ts              # Default export — nested children
├── dashboard-page/
│   └── dashboard-page.ts         # SellerDashboardPageComponent — shell with nav
├── product-list/
│   └── product-list.ts           # SellerProductListComponent — seller's products
├── product-form/
│   ├── product-form.ts           # ProductFormComponent — create/edit product
│   └── product-form.html         # External template
├── variant-matrix-builder/
│   └── variant-matrix-builder.ts # VariantMatrixBuilderComponent — SKU variant grid
├── seller-orders/
│   ├── seller-orders.ts          # SellerOrdersComponent — orders for seller's store
│   └── seller-orders.spec.ts     # ✅ Tests
├── store-settings/
│   └── store-settings.ts         # StoreSettingsComponent — store name/description/logo
├── inventory-list/
│   ├── inventory-list.ts         # InventoryListComponent — stock levels
│   └── inventory-list.spec.ts    # ✅ Tests
└── components/
    └── sales-card/
        └── sales-card.ts         # SalesCardComponent — sales summary card
```

## Models

Defined in `seller.models.ts`:

```typescript
interface SellerProduct {
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

interface StoreSettings {
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

interface SalesSummary {
  totalOrders: number;
  totalRevenue: number;
  pendingOrders: number;
  completedOrders: number;
}

interface CreateProductRequest {
  name: string;
  description: string;
  brand?: string;
  categoryId: string;
  storeId: string;
  tags?: string[];
  imageUrl?: string;
}

interface UpdateProductRequest {
  name: string;
  description: string;
  categoryId: string;
  brand?: string;
  tags?: string[];
  imageUrl?: string;
}

interface AddSkuRequest {
  skuCode: string;
  price: number;
  currency: string;
  typedAttributes?: Record<string, string>;
  flexibleAttributes?: Record<string, string>;
}

interface BulkAddSkuRequest {
  variantCombinations: Record<string, string[]>;
  basePrice?: number;
  currency?: string;
  excludedCombinations?: string[];
  skuCodePrefix?: string;
}

interface BulkAddSkuResult {
  createdCount: number;
  totalCombinations: number;
  createdSkus: Sku[];
  errors?: string[];
}
```

## SignalStore State Management

### SellerProductStore (root singleton)

| State Property | Type | Description |
|:---|:---|:---|
| `products` | `SellerProduct[]` | Seller's products with SKUs |
| `selectedProduct` | `SellerProduct \| null` | Product being edited |
| `loading` | `boolean` | Loading state |
| `error` | `string \| null` | Error message |

**Computed signals:** `activeProducts`, `inactiveProducts`, `hasProducts`

**Key methods:** `loadProducts()`, `loadProductById(id)`, `createProduct(request)`, `updateProduct(id, request)`, `deleteProduct(id)`, `activateProduct(id)`, `deactivateProduct(id)`, `addSku(productId, request)`, `bulkAddSku(productId, request)`, `removeSku(productId, skuId)`, `clearSelected()`

### StoreSettingsStore (root singleton)

| State Property | Type | Description |
|:---|:---|:---|
| `settings` | `StoreSettings \| null` | Store configuration |
| `salesSummary` | `SalesSummary \| null` | Sales data |
| `loading` | `boolean` | Loading state |
| `error` | `string \| null` | Error message |

**Computed signals:** `hasSettings`, `storeId`

**Key methods:** `loadSettings()`, `createStore(name, description)`, `updateSettings(name, description)`, `setLogo(logoUrl)`, `loadSalesSummary()`

**Note:** `storeId` persisted in `localStorage` after load/create. 404 on `loadSettings()` is treated as "no store yet" (not an error).

### InventoryStore (root singleton)

| State Property | Type | Description |
|:---|:---|:---|
| `items` | `InventoryDisplayItem[]` | SKU-level stock data joined with product info |
| `loading` | `boolean` | Loading state |
| `error` | `string \| null` | Error message |

**Computed signals:** `lowStockItems`, `lowStockCount`

**Key methods:** `loadInventory()`, `addStock(sku, quantity)`

**Low stock threshold:** 5 units

## Key Routes

| Path | Component | Guard |
|:---|:---|:---|
| `/seller` | `SellerDashboardPageComponent` | `authGuard` + `roleGuard('Seller', 'Admin')` (parent) |
| `/seller` → redirect | → `/seller/products` | |
| `/seller/products` | `SellerProductListComponent` | (inherited) |
| `/seller/products/new` | `ProductFormComponent` | (inherited) |
| `/seller/products/:id/edit` | `ProductFormComponent` | (inherited) |
| `/seller/orders` | `SellerOrdersComponent` | (inherited) |
| `/seller/inventory` | `InventoryListComponent` | (inherited) |
| `/seller/settings` | `StoreSettingsComponent` | (inherited) |

## Test Coverage Status

| Spec File | Tests | Status |
|:---|:---|:---|
| `seller-product.store.spec.ts` | ✅ | Passing |
| `seller-product.service.spec.ts` | ✅ | Passing |
| `inventory.store.spec.ts` | ✅ | Passing |
| `inventory.service.spec.ts` | ✅ | Passing |
| `seller-orders/seller-orders.spec.ts` | ✅ | Passing |
| `inventory-list/inventory-list.spec.ts` | ✅ | Passing |
| `store-settings.store.spec.ts` | ❌ | **No tests** |
| `dashboard-page/` | ❌ | **No tests** |
| `product-list/` | ❌ | **No tests** |
| `product-form/` | ❌ | **No tests** |
| `variant-matrix-builder/` | ❌ | **No tests** |
| `store-settings/` | ❌ | **No tests** |

**E2E Coverage:** Partially covered — `seller-dashboard.spec.ts` (~4 tests). Only dashboard display. Missing: product CRUD, store settings, orders, inventory management, correlation.

## Known Gaps / Issues

- **StoreSettingsStore has 0 unit tests.**
- **Product form, product list, and variant matrix builder have 0 component tests.**
- **Store creation flow:** `StoreSettingsComponent` must handle the case where seller has no store yet — `loadSettings()` returns null on 404, but the UI must show a "create store" form.
- **Inventory join logic:** `InventoryStore.loadInventory()` fetches products via `SellerProductService`, then inventory via `SellerInventoryService`, then joins them client-side. This is a two-API-call pattern with no caching.
- **`SellerProductStore` depends on `StoreSettingsStore`:** `loadProducts()` reads `storeId()` from `StoreSettingsStore` — must be loaded first.
- **SKU management:** `addSku()` and `removeSku()` update local state optimistically but don't reload the full product. `bulkAddSku()` reloads the product after bulk creation.
