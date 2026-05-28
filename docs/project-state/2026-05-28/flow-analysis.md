# Flow Analysis — 2026-05-28

> **Snapshot Date:** 2026-05-28  
> **Purpose:** End-to-end flow analysis for key marketplace features

---

## Flow 1: User Registration & Login ✅

```
Frontend → POST /bff/auth/register → Gateway → Identity.API → JWT → Cookie
Frontend → POST /bff/auth/login → Gateway → Identity.API → JWT → Cookie
```

| Step | Component | Status |
|:---:|:---|:---:|
| 1 | LoginComponent / RegisterComponent | ✅ |
| 2 | AuthStore.login/register | ✅ |
| 3 | POST /bff/auth/* | ✅ |
| 4 | Gateway CookieToBearer middleware | ✅ |
| 5 | Identity.API validates & returns JWT | ✅ |
| 6 | Cookie set, subsequent requests authenticated | ✅ |

**Verdict:** ✅ Working end-to-end

---

## Flow 2: Browse Products ✅

```
Frontend → GET /api/catalog/products → Catalog.API → PostgreSQL → ProductListDto
```

| Step | Component | Status |
|:---:|:---|:---:|
| 1 | ProductListComponent | ✅ |
| 2 | CatalogStore.loadProducts() | ✅ |
| 3 | CatalogService.getProducts() | ✅ |
| 4 | GET /api/catalog/products?category=...&search=... | ✅ |
| 5 | Catalog.API queries with filters | ✅ |
| 6 | Returns ProductListItem (with defaultSkuId/defaultSkuCode) | ✅ |
| 7 | ProductCardComponent displays | ✅ |

**Verdict:** ✅ Working end-to-end

---

## Flow 3: View Product Detail ✅

```
Frontend → GET /bff/catalog/products/{id} → Gateway → Catalog.API
Frontend → GET /bff/catalog/skus/{skuId}/gallery → Gateway → Media.API
```

| Step | Component | Status |
|:---:|:---|:---:|
| 1 | ProductDetailComponent | ✅ |
| 2 | ProductDetailStore.loadProduct(id) | ✅ |
| 3 | GET /bff/catalog/products/{id} (BFF merges data) | ✅ |
| 4 | GET /api/media/gallery/SKU/{skuId} | ✅ |
| 5 | ImageGalleryComponent displays images | ✅ |
| 6 | BuyBoxComponent shows price + add to cart | ✅ |
| 7 | ReviewListComponent shows reviews | ✅ |

**Verdict:** ✅ Working end-to-end

---

## Flow 4: Add to Cart ✅

```
Frontend → POST /api/cart/items → Cart.API → PostgreSQL
```

| Step | Component | Status |
|:---:|:---|:---:|
| 1 | BuyBoxComponent (addToCart event) | ✅ |
| 2 | CartStore.addToCart(productId, skuId, skuCode, qty) | ✅ |
| 3 | CartService.addItem() | ✅ |
| 4 | POST /api/cart/items {productId, skuId, skuCode, quantity} | ✅ |
| 5 | Cart.API validates SKU, creates/updates item | ✅ |
| 6 | MiniCartComponent updates count | ✅ |

**Verdict:** ✅ Working end-to-end (SKU integration complete)

---

## Flow 5: Checkout ✅

```
Frontend → POST /api/cart/checkout → Cart.API → CheckoutRequestedEvent
  → Ordering.Saga → Inventory.Reserve → Payment.Process → Order.Create
```

| Step | Component | Status |
|:---:|:---|:---:|
| 1 | CheckoutPageComponent | ✅ |
| 2 | CheckoutStore.checkout(address) | ✅ |
| 3 | CartService.checkout() | ✅ |
| 4 | POST /api/cart/checkout | ✅ |
| 5 | Cart.API publishes CheckoutRequestedEvent | ✅ |
| 6 | Ordering saga orchestrates: | ✅ |
| 6a | → Inventory: ReserveStock | ✅ |
| 6b | → Payment: ProcessPayment | ✅ |
| 6c | → Ordering: CreateOrder | ✅ |
| 7 | CheckoutStatusComponent shows result | ✅ |

**Verdict:** ✅ Working end-to-end (saga with compensation)

---

## Flow 6: Order Management ✅

```
Frontend → GET /bff/orders/buyer/{buyerId} → Gateway → Ordering.API
Frontend → POST /api/orders/{id}/cancel → Ordering.API → compensation
```

| Step | Component | Status |
|:---:|:---|:---:|
| 1 | OrderListComponent | ✅ |
| 2 | OrderStore.loadOrders(buyerId) | ✅ |
| 3 | GET /bff/orders/buyer/{buyerId} (via BFF) | ✅ |
| 4 | OrderDetailComponent | ✅ |
| 5 | GET /bff/orders/{id} | ✅ |
| 6 | POST /api/orders/{id}/cancel | ✅ |
| 7 | Ordering.API triggers compensation | ✅ |
| 8 | Inventory: ReleaseStock | ✅ |
| 9 | Payment: Refund | ✅ |

**Verdict:** ✅ Working end-to-end

---

## Flow 7: Seller Creates Product & SKU ⚠️

```
Frontend → POST /api/catalog/products → Catalog.API → ProductCreatedEvent
Frontend → POST /api/catalog/products/{id}/skus → Catalog.API → SkuCreatedEvent
  → Inventory consumer ✅
  → Search consumer ❌
  → Cart consumer ❌
```

| Step | Component | Status |
|:---:|:---|:---:|
| 1 | ProductFormComponent | ✅ |
| 2 | SellerProductStore.createProduct() | ✅ |
| 3 | POST /api/catalog/products | ✅ |
| 4 | Catalog.API creates product | ✅ |
| 5 | ProductCreatedEvent published | ✅ |
| 6 | SellerProductStore.addSku() | ✅ |
| 7 | POST /api/catalog/products/{id}/skus | ✅ |
| 8 | Catalog.API creates SKU | ✅ |
| 9 | SkuCreatedIntegrationEvent published | ✅ |
| 10 | Inventory.SkuCreatedConsumer creates stock | ✅ |
| 11 | Search.SkuCreatedConsumer updates price | ❌ Missing |
| 12 | Cart.SkuCreatedConsumer updates cache | ❌ Missing |

**Gaps:**
- Search doesn't consume SkuCreatedIntegrationEvent
- Cart doesn't consume SkuCreatedIntegrationEvent

**Verdict:** ⚠️ Product/SKU created, inventory updated, but Search/Cart not synced

---

## Flow 8: Seller Manages Inventory ✅

```
Frontend → POST /api/inventory/items/{sku}/add-stock → Inventory.API
Frontend → POST /api/inventory/items/batch → Inventory.API (stock levels)
```

| Step | Component | Status |
|:---:|:---|:---:|
| 1 | InventoryListComponent | ✅ |
| 2 | InventoryStore.loadStock() | ✅ |
| 3 | POST /api/inventory/items/batch | ✅ |
| 4 | InventoryService.addStock() | ✅ |
| 5 | POST /api/inventory/items/{sku}/add-stock | ✅ |

**Verdict:** ✅ Working end-to-end

---

## Flow 9: Search Products ❌

```
Frontend → GET /api/search/products?q=... → Search.API → Elasticsearch
```

| Step | Component | Status |
|:---:|:---|:---:|
| 1 | SearchFacetsComponent | ✅ |
| 2 | CatalogService.searchProducts() | ✅ |
| 3 | GET /api/search/products?q=... | ✅ |
| 4 | Search.API queries Elasticsearch | ✅ |
| 5 | Returns results | ✅ |

**Gaps:**
- ❌ Search document is product-level, not SKU-level
- ❌ No SkuCreatedIntegrationEvent consumer (price stale)
- ❌ Contract test failing

**Verdict:** ❌ Basic search works, but SKU-level indexing missing

---

## Flow 10: Admin Manages Users & Stores ✅

```
Frontend → GET /api/identity/users → Identity.API
Frontend → POST /api/stores/{id}/verify → StoreManagement.API
```

| Step | Component | Status |
|:---:|:---|:---:|
| 1 | UserListComponent | ✅ |
| 2 | AdminStore.loadUsers() | ✅ |
| 3 | GET /api/identity/users | ✅ |
| 4 | PUT /api/identity/users/{id}/role | ✅ |
| 5 | StoreVerificationComponent | ✅ |
| 6 | POST /api/stores/{id}/verify | ✅ |

**Verdict:** ✅ Working end-to-end

---

## Summary

| Flow | Status | Frontend | Backend | Integration |
|:---|:---:|:---:|:---:|:---:|
| Registration & Login | ✅ | ✅ | ✅ | ✅ |
| Browse Products | ✅ | ✅ | ✅ | ✅ |
| View Product Detail | ✅ | ✅ | ✅ | ✅ |
| Add to Cart | ✅ | ✅ | ✅ | ✅ |
| Checkout | ✅ | ✅ | ✅ | ✅ |
| Order Management | ✅ | ✅ | ✅ | ✅ |
| Seller Create Product/SKU | ⚠️ | ✅ | ✅ | ⚠️ |
| Seller Manage Inventory | ✅ | ✅ | ✅ | ✅ |
| Search Products | ❌ | ✅ | ✅ | ❌ |
| Admin Management | ✅ | ✅ | ✅ | ✅ |

**8/10 flows fully working, 1 partial (Search sync), 1 has gaps (Search indexing)**
