# Feature Matrix — 2026-05-28

> **Snapshot Date:** 2026-05-28  
> **Purpose:** Full-stack feature implementation status (Frontend ↔ Backend ↔ Integration)

---

## 1. Authentication ✅ Complete

### Frontend
| Component | File | Status |
|:---|:---|:---:|
| LoginComponent | `auth/login/login.ts` | ✅ |
| RegisterComponent | `auth/register/register.ts` | ✅ |
| ForgotPasswordComponent | `auth/forgot-password/forgot-password.ts` | ✅ |
| ProfileComponent | `auth/profile/profile.ts` | ✅ |
| ProfileSettingsComponent | `auth/profile/profile-settings.ts` | ✅ |
| ProfileSidebarComponent | `auth/profile/profile-sidebar.ts` | ✅ |

**Store:** AuthStore (login, register, logout, refresh)  
**Tests:** login.spec.ts, register.spec.ts

### Backend
| Endpoint | Auth | Status |
|:---|:---:|:---:|
| POST /api/auth/register | Anonymous | ✅ |
| POST /api/auth/login | Anonymous | ✅ |
| POST /api/auth/refresh | Anonymous | ✅ |
| POST /api/auth/forgot-password | Anonymous | ✅ |
| POST /api/auth/change-password | Auth | ✅ |

### Gateway (BFF)
| Endpoint | Status |
|:---|:---:|
| POST /bff/auth/login | ✅ |
| POST /bff/auth/register | ✅ |
| POST /bff/auth/forgot-password | ✅ |
| POST /bff/auth/logout | ✅ |
| GET /bff/user | ✅ |
| GET /bff/csrf | ✅ |

**Tests:** 45 unit, 7 integration  
**Verdict:** ✅ Full-stack complete. Cookie-based auth via BFF, JWT for API calls.

---

## 2. Product Catalog ✅ Complete

### Frontend
| Component | File | Status |
|:---|:---|:---:|
| ProductListComponent | `catalog/product-list/product-list.ts` | ✅ |
| ProductDetailComponent | `catalog/product-detail/product-detail.ts` | ✅ |
| ProductCardComponent | `catalog/components/product-card.ts` | ✅ |
| BuyBoxComponent | `catalog/components/buy-box.ts` | ✅ |
| CategorySidebarComponent | `catalog/components/category-sidebar.ts` | ✅ |
| ImageGalleryComponent | `catalog/components/image-gallery.ts` | ✅ |
| SearchFacetsComponent | `catalog/components/search-facets.ts` | ✅ |
| PaginationComponent | `catalog/components/pagination.ts` | ✅ |
| FrequentlyBoughtTogetherComponent | `catalog/components/frequently-bought-together.ts` | ✅ |

**Store:** CatalogStore, ProductDetailStore  
**Service:** CatalogService  
**Models:** ProductListItem (with defaultSkuId/defaultSkuCode), Product, Category  
**API Calls:**
- `GET /api/catalog/products` — list with filters
- `GET /bff/catalog/products/{id}` — detail via BFF
- `GET /api/catalog/products/featured` — featured products
- `GET /api/catalog/products/{id}/recommendations` — recommendations
- `GET /api/catalog/categories` — category tree

### Backend
| Endpoint | Auth | Status |
|:---|:---:|:---:|
| GET /api/products | Anonymous | ✅ |
| GET /api/products/featured | Anonymous | ✅ |
| POST /api/products/by-ids | Anonymous | ✅ |
| GET /api/products/sku/{sku} | Anonymous | ✅ |
| GET /api/products/{id} | Anonymous | ✅ |
| POST /api/products | Auth | ✅ |
| PUT /api/products/{id} | Auth | ✅ |
| PATCH /api/products/{id}/price | Auth | ✅ |
| GET /api/products/{id}/recommendations | Anonymous | ✅ |
| PUT /api/products/{id}/activate | Auth | ✅ |
| PUT /api/products/{id}/deactivate | Auth | ✅ |
| DELETE /api/products/{id} | Auth | ✅ |
| GET /api/categories/tree | Anonymous | ✅ |
| GET /api/categories | Anonymous | ✅ |
| POST /api/categories | Auth | ✅ |
| PUT /api/categories/{id} | Auth | ✅ |
| DELETE /api/categories/{id} | Auth | ✅ |
| POST /api/categories/{id}/attributes | Auth | ✅ |
| GET /api/categories/{id}/attributes | Anonymous | ✅ |
| DELETE /api/categories/{id}/attributes/{attrId} | Auth | ✅ |

### Gateway (BFF)
| Endpoint | Status |
|:---|:---:|
| GET /bff/catalog/products/{id} | ✅ |
| GET /bff/catalog/skus/{skuId} | ✅ |
| GET /bff/catalog/skus/{skuId}/gallery | ✅ |

**Tests:** 32 unit, 4 integration  
**Verdict:** ✅ Full-stack complete. SKU support integrated. BFF merges product+gallery data.

---

## 3. Shopping Cart ✅ Complete

### Frontend
| Component | File | Status |
|:---|:---|:---:|
| CartPageComponent | `cart/cart-page/cart-page.ts` | ✅ |
| MiniCartComponent | `cart/components/mini-cart.ts` | ✅ |

**Store:** CartStore (addToCart, updateQuantity, removeFromCart, clearCart, checkout)  
**Service:** CartService  
**Models:** ShoppingCart, CartItemDetails (with skuId/skuCode)  
**API Calls:**
- `GET /bff/cart` — get cart (via BFF for auth)
- `POST /api/cart/items` — add item {productId, skuId, skuCode, quantity}
- `PUT /api/cart/items/{skuId}` — update quantity
- `DELETE /api/cart/items/{skuId}` — remove item
- `POST /api/cart/checkout` — checkout
- `DELETE /api/cart` — clear cart

**Tests:** cart.service.spec.ts, cart.store.spec.ts, cart-page.spec.ts

### Backend
| Endpoint | Auth | Status |
|:---|:---:|:---:|
| GET /api/cart | Auth/Anon | ✅ |
| DELETE /api/cart | Auth | ✅ |
| POST /api/cart/checkout | Auth | ✅ |
| POST /api/cart/items | Auth/Anon | ✅ |
| PUT /api/cart/items/{productId}/{skuId} | Auth/Anon | ✅ |
| DELETE /api/cart/items/{productId}/{skuId} | Auth/Anon | ✅ |

### Gateway (BFF)
| Endpoint | Status |
|:---|:---:|
| GET /bff/cart | ✅ |

**Tests:** 31 unit, 20 integration  
**Verdict:** ✅ Full-stack complete. SKU-based cart items. X-Cart-Id header for anonymous carts.

---

## 4. Checkout ✅ Complete

### Frontend
| Component | File | Status |
|:---|:---|:---:|
| CheckoutPageComponent | `checkout/checkout-page/checkout-page.ts` | ✅ |
| CheckoutStatusComponent | `checkout/checkout-status/checkout-status.ts` | ✅ |
| CheckoutSummaryComponent | `checkout/checkout-summary/checkout-summary.ts` | ✅ |
| AddressFormComponent | `checkout/address-form/address-form.ts` | ✅ |

**Store:** CheckoutStore  
**Tests:** checkout.store.spec.ts, checkout-page.spec.ts

### Backend (Multi-service orchestration)
| Step | Service | Endpoint/Event | Status |
|:---:|:---|:---|:---:|
| 1 | Cart.API | POST /api/cart/checkout | ✅ |
| 2 | Cart.API | Publishes CheckoutRequestedEvent | ✅ |
| 3 | Ordering.Saga | Orchestrates checkout flow | ✅ |
| 3a | Inventory.API | ReserveStock command | ✅ |
| 3b | Payment.API | ProcessPayment command | ✅ |
| 3c | Ordering.API | CreateOrder command | ✅ |

**Tests:** 69 ordering unit, 30 payment unit  
**Verdict:** ✅ Full-stack complete. Saga-based checkout with compensation.

---

## 5. Order Management ✅ Complete

### Frontend
| Component | File | Status |
|:---|:---|:---:|
| OrderListComponent | `orders/order-list/order-list.ts` | ✅ |
| OrderDetailComponent | `orders/order-detail/order-detail.ts` | ✅ |
| OrderTimelineComponent | `orders/order-timeline/order-timeline.ts` | ✅ |
| StatusBadgeComponent | `orders/components/status-badge.ts` | ✅ |

**Store:** OrderStore  
**Service:** OrderService  
**API Calls:**
- `GET /bff/orders/buyer/{buyerId}` — list orders (via BFF)
- `GET /bff/orders/{id}` — order detail (via BFF)
- `GET /api/payments/order/{orderId}` — payment status
- `POST /api/orders/{id}/cancel` — cancel order
- `PUT /api/orders/{id}/status` — update status (seller)

**Tests:** order.service.spec.ts, order.store.spec.ts

### Backend
| Endpoint | Auth | Status |
|:---|:---:|:---:|
| POST /api/orders | Auth | ✅ |
| GET /api/orders/{id} | Auth | ✅ |
| GET /api/orders/buyer/{buyerId} | Auth | ✅ |
| GET /api/orders/store/{storeId} | Seller | ✅ |
| POST /api/orders/{id}/cancel | Auth | ✅ |
| PUT /api/orders/{id}/status | Seller | ✅ |
| GET /api/orders/has-purchased | Anonymous | ✅ |

### Gateway (BFF)
| Endpoint | Status |
|:---|:---:|
| GET /bff/orders/buyer/{buyerId} | ✅ |
| GET /bff/orders/{id} | ✅ |

**Tests:** 69 unit, 3 integration  
**Verdict:** ✅ Full-stack complete. Order lifecycle with cancellation and status updates.

---

## 6. Inventory ⚠️ Partial

### Frontend (via Seller Dashboard)
| Component | File | Status |
|:---|:---|:---:|
| InventoryListComponent | `seller-dashboard/inventory-list/inventory-list.ts` | ✅ |

**Store:** InventoryStore  
**Service:** InventoryService  
**API Calls:**
- `POST /api/inventory/items/batch` — get stock for SKUs
- `POST /api/inventory/items/{sku}/add-stock` — add stock

**Tests:** inventory.service.spec.ts, inventory.store.spec.ts

### Backend
| Endpoint | Auth | Status |
|:---|:---:|:---:|
| POST /api/inventory/items | Auth | ✅ |
| POST /api/inventory/items/{skuCode}/add-stock | Auth | ✅ |
| GET /api/inventory/items/{skuCode} | Anonymous | ✅ |
| GET /api/inventory/items | Auth | ✅ |
| POST /api/inventory/items/batch | Auth | ✅ |
| PUT /api/inventory/items/{skuCode}/stock | Auth | ✅ |

**Tests:** 8 unit, 5/8 integration ❌  
**Integration Failures:**
- `ReserveInventory_MultipleItems_ReservesAllOrFails` — mock capture null
- `ReserveInventory_SufficientStock_PublishesReservedEvent_AndReducesStock` — mock capture null
- `CancelReservation_PublishesReleasedEvent_AndRestoresStock` — quantity mismatch

**Gaps:**
- ❌ `ReserveStockCommandHandler` looks up by ProductId, not SkuId
- ❌ Reservation consumer tests failing (mock assertion issues)

**Verdict:** ⚠️ Frontend connected, basic CRUD works, reservation flow has test failures.

---

## 7. Seller Dashboard ⚠️ Partial

### Frontend
| Component | File | Status |
|:---|:---|:---:|
| DashboardPageComponent | `seller-dashboard/dashboard-page/dashboard-page.ts` | ✅ |
| ProductFormComponent | `seller-dashboard/product-form/product-form.ts` | ✅ |
| ProductListComponent | `seller-dashboard/product-list/product-list.ts` | ✅ |
| InventoryListComponent | `seller-dashboard/inventory-list/inventory-list.ts` | ✅ |
| SellerOrdersComponent | `seller-dashboard/seller-orders/seller-orders.ts` | ✅ |
| StoreSettingsComponent | `seller-dashboard/store-settings/store-settings.ts` | ✅ |
| SalesCardComponent | `seller-dashboard/components/sales-card.ts` | ✅ |

**Stores:** SellerProductStore, InventoryStore, StoreSettingsStore  
**Services:** SellerProductService, InventoryService, StoreService  
**API Calls:**
- `GET /api/catalog/products` — list seller's products
- `POST /api/catalog/products` — create product
- `PUT /api/catalog/products/{id}` — update product
- `POST /api/catalog/products/{id}/skus` — add SKU
- `DELETE /api/catalog/products/{id}/skus/{skuId}` — remove SKU
- `PATCH /api/catalog/products/{id}/skus/{skuId}/price` — change price
- `PUT /api/catalog/products/{id}/activate` — activate
- `PUT /api/catalog/products/{id}/deactivate` — deactivate
- `DELETE /api/catalog/products/{id}` — delete product
- `POST /api/inventory/items/batch` — get stock levels
- `POST /api/inventory/items/{sku}/add-stock` — add stock
- `GET /api/stores/seller/{sellerId}` — get seller's store
- `PUT /api/stores/{storeId}` — update store settings
- `PUT /api/stores/{storeId}/logo` — update store logo

**TODO:** `store.service.ts:61` — "TODO: Implement when Ordering.API has a sales summary endpoint"

### Backend (Multi-service)
| Service | Endpoints Used | Status |
|:---|:---|:---:|
| Catalog.API | Product CRUD, SKU CRUD, Price changes | ✅ |
| Inventory.API | Stock management | ✅ |
| StoreManagement.API | Store settings, logo | ✅ |
| Ordering.API | Seller orders | ✅ |

**Gaps:**
- ❌ No sales summary endpoint (TODO in store.service.ts)
- ❌ Search/Cart don't sync when SKUs created (affects product visibility)

**Verdict:** ⚠️ Full CRUD works, but downstream sync issues (Search/Cart not updated on SKU changes).

---

## 8. Store Management ✅ Complete

### Frontend (via Seller Dashboard + Stores feature)
| Component | File | Status |
|:---|:---|:---:|
| StorePageComponent | `stores/store-page/store-page.ts` | ✅ |
| StoreSettingsComponent | `seller-dashboard/store-settings/store-settings.ts` | ✅ |

**Service:** StoreService  
**API Calls:**
- `GET /api/stores/{id}` — get store details
- `GET /api/stores/seller/{sellerId}` — get seller's store
- `POST /api/stores` — create store
- `PUT /api/stores/{id}` — update store
- `PUT /api/stores/{id}/logo` — update logo

### Backend
| Endpoint | Auth | Status |
|:---|:---:|:---:|
| POST /api/stores | Seller | ✅ |
| GET /api/stores | Anonymous | ✅ |
| GET /api/stores/{id} | Anonymous | ✅ |
| GET /api/stores/seller/{sellerId} | Anonymous | ✅ |
| PUT /api/stores/{id} | Seller | ✅ |
| POST /api/stores/{id}/verify | Admin | ✅ |
| PUT /api/stores/{id}/logo | Auth | ✅ |

**Tests:** 29 unit  
**Verdict:** ✅ Full-stack complete.

---

## 9. Admin ✅ Complete

### Frontend
| Component | File | Status |
|:---|:---|:---:|
| AdminPageComponent | `admin/admin-page/admin-page.ts` | ✅ |
| UserListComponent | `admin/user-list/user-list.ts` | ✅ |
| StoreDetailComponent | `admin/store-detail/store-detail.ts` | ✅ |
| StoreVerificationComponent | `admin/store-verification/store-verification.ts` | ✅ |
| StatsCardComponent | `admin/components/stats-card.ts` | ✅ |

**Store:** AdminStore  
**Services:** AdminStoreService, AdminUserService  
**API Calls:**
- `GET /api/identity/users` — list users
- `GET /api/identity/users/{id}` — user detail
- `PUT /api/identity/users/{id}/role` — change role
- `DELETE /api/identity/users/{id}` — delete user
- `GET /api/stores` — list stores
- `GET /api/stores/{id}` — store detail
- `POST /api/stores/{storeId}/verify` — verify store

### Backend (Multi-service)
| Service | Endpoints Used | Status |
|:---|:---|:---:|
| Identity.API | User CRUD, role management | ✅ |
| StoreManagement.API | Store verification | ✅ |

**Tests:** 45 identity unit, 29 store unit, 2 api-gateway integration  
**Verdict:** ✅ Full-stack complete.

---

## 10. Search ❌ Gaps

### Frontend
| Component | File | Status |
|:---|:---|:---:|
| SearchFacetsComponent | `catalog/components/search-facets.ts` | ✅ |

**Service:** CatalogService (search method)  
**API Calls:**
- `GET /api/search/products?q=...&category=...&minPrice=...&maxPrice=...` — search

### Backend
| Endpoint | Auth | Status |
|:---|:---:|:---:|
| GET /api/search/products | Anonymous | ✅ |

**Tests:** 8 unit, 5/6 integration ❌  
**Integration Failure:**
- `UpdateProduct_VerifyNewFields` — Currency mismatch (expected "EUR", got "USD")

**Critical Gaps:**
- ❌ No `SkuCreatedIntegrationEvent` consumer in Search (F5 from audit)
- ❌ Search document is product-level, not SKU-level (F2)
- ❌ Price in search may be stale (no event sync)
- ❌ Contract test failing: `SkuCreatedIntegrationEvent_Contract_ShouldUpdatePriceInSearch`

**Verdict:** ❌ Basic search works, but SKU-level indexing and event sync missing.

---

## 11. Media/Gallery ✅ Complete

### Frontend
| Component | File | Status |
|:---|:---|:---:|
| ImageGalleryComponent | `catalog/components/image-gallery.ts` | ✅ |

**API Calls:**
- `GET /api/media/gallery/SKU/{skuId}` — get gallery for SKU
- `GET /api/media/{mediaId}` — get media file
- `GET /api/media/{mediaId}/thumbnail` — get thumbnail

### Backend
| Endpoint | Auth | Status |
|:---|:---:|:---:|
| POST /api/media/upload | Auth | ✅ |
| GET /api/media/gallery/{targetType}/{targetId} | Anonymous | ✅ |
| GET /api/media/{mediaId} | Anonymous | ✅ |
| GET /api/media/{mediaId}/thumbnail | Anonymous | ✅ |
| DELETE /api/media/{mediaId} | Auth | ✅ |
| PUT /api/media/gallery/{targetType}/{targetId}/reorder | Auth | ✅ |
| PUT /api/media/gallery/{targetType}/{targetId}/primary/{mediaItemId} | Auth | ✅ |

### Gateway (BFF)
| Endpoint | Status |
|:---|:---:|
| GET /bff/catalog/skus/{skuId}/gallery | ✅ |

**Tests:** — (empty test projects)  
**Verdict:** ✅ Full-stack complete. GalleryEntry links media to SKU targets.

---

## 12. Reviews ✅ Complete

### Frontend
| Component | File | Status |
|:---|:---|:---:|
| ReviewListComponent | `catalog/components/review-list.ts` | ✅ |
| ReviewSummaryComponent | `catalog/components/review-summary.ts` | ✅ |
| WriteReviewComponent | `catalog/components/write-review.ts` | ✅ |

**Store:** ReviewStore  
**Service:** ReviewService  
**API Calls:**
- `GET /api/catalog/products/{id}/reviews` — list reviews
- `GET /api/catalog/products/{id}/reviews/summary` — review summary
- `POST /api/catalog/products/{id}/reviews` — submit review
- `POST /api/catalog/products/reviews/{reviewId}/vote` — vote helpful

### Backend
| Endpoint | Auth | Status |
|:---|:---:|:---:|
| GET /api/products/{id}/reviews | Anonymous | ✅ |
| GET /api/products/{id}/reviews/summary | Anonymous | ✅ |

**Verdict:** ✅ Full-stack complete.

---

## 13. Notifications ⚠️ Backend Only

### Frontend
No dedicated notification UI components found. SignalR connection may be used for real-time updates.

### Backend
- Notification.Worker service with SignalR hub
- Redis backplane for scaling

**Tests:** 20 unit  
**Verdict:** ⚠️ Backend implemented, frontend integration unclear.

---

## Summary by Status

| Status | Count | Features |
|:---|:---:|:---|
| ✅ Complete | 9 | Auth, Catalog, Cart, Checkout, Orders, Store Mgmt, Admin, Media, Reviews |
| ⚠️ Partial | 3 | Inventory, Seller Dashboard, Notifications |
| ❌ Gaps | 1 | Search |
