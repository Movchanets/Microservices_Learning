# Frontend State — 2026-05-28

> **Snapshot Date:** 2026-05-28  
> **Stack:** Angular 20, Vitest

---

## Test Summary

| Metric | Value |
|:---|:---|
| Spec Files | 36 passed |
| Total Tests | 344 passed |
| Duration | ~7s |

---

## Feature Modules

### 1. Auth (6 components)
| Component | Status |
|:---|:---:|
| LoginComponent | ✅ |
| RegisterComponent | ✅ |
| ForgotPasswordComponent | ✅ |
| ProfileComponent | ✅ |
| ProfileSettingsComponent | ✅ |
| ProfileSidebarComponent | ✅ |

**Store:** AuthStore | **Tests:** login.spec.ts, register.spec.ts

---

### 2. Catalog (12 components)
| Component | Status |
|:---|:---:|
| ProductListComponent | ✅ |
| ProductDetailComponent | ✅ |
| ProductCardComponent | ✅ |
| BuyBoxComponent | ✅ |
| CategorySidebarComponent | ✅ |
| ImageGalleryComponent | ✅ |
| SearchFacetsComponent | ✅ |
| PaginationComponent | ✅ |
| FrequentlyBoughtTogetherComponent | ✅ |
| ReviewListComponent | ✅ |
| ReviewSummaryComponent | ✅ |
| WriteReviewComponent | ✅ |

**Stores:** CatalogStore, ProductDetailStore, ReviewStore  
**Services:** CatalogService, ReviewService

---

### 3. Cart (2 components)
| Component | Status |
|:---|:---:|
| CartPageComponent | ✅ |
| MiniCartComponent | ✅ |

**Store:** CartStore | **Service:** CartService  
**Tests:** cart.service.spec.ts, cart.store.spec.ts, cart-page.spec.ts

---

### 4. Checkout (4 components)
| Component | Status |
|:---|:---:|
| CheckoutPageComponent | ✅ |
| CheckoutStatusComponent | ✅ |
| CheckoutSummaryComponent | ✅ |
| AddressFormComponent | ✅ |

**Store:** CheckoutStore | **Tests:** checkout.store.spec.ts, checkout-page.spec.ts

---

### 5. Orders (4 components)
| Component | Status |
|:---|:---:|
| OrderListComponent | ✅ |
| OrderDetailComponent | ✅ |
| OrderTimelineComponent | ✅ |
| StatusBadgeComponent | ✅ |

**Store:** OrderStore | **Service:** OrderService  
**Tests:** order.service.spec.ts, order.store.spec.ts

---

### 6. Home (5 components)
| Component | Status |
|:---|:---:|
| HomePageComponent | ✅ |
| HeroBannerComponent | ✅ |
| CategoryTilesComponent | ✅ |
| ProductCarouselComponent | ✅ |
| DealOfTheDayComponent | ✅ |

**Store:** HomeStore

---

### 7. Stores (1 component)
| Component | Status |
|:---|:---:|
| StorePageComponent | ✅ |

---

### 8. Seller Dashboard (7 components)
| Component | Status |
|:---|:---:|
| DashboardPageComponent | ✅ |
| ProductFormComponent | ✅ |
| ProductListComponent | ✅ |
| InventoryListComponent | ✅ |
| SellerOrdersComponent | ✅ |
| StoreSettingsComponent | ✅ |
| SalesCardComponent | ✅ |

**Stores:** SellerProductStore, InventoryStore, StoreSettingsStore  
**Services:** SellerProductService, InventoryService, StoreService  
**TODO:** `store.service.ts:61` — "Implement when Ordering.API has a sales summary endpoint"

---

### 9. Admin (5 components)
| Component | Status |
|:---|:---:|
| AdminPageComponent | ✅ |
| UserListComponent | ✅ |
| StoreDetailComponent | ✅ |
| StoreVerificationComponent | ✅ |
| StatsCardComponent | ✅ |

**Store:** AdminStore | **Services:** AdminStoreService, AdminUserService

---

### 10. Profile (1 component)
| Component | Status |
|:---|:---:|
| SavedSearchesComponent | ✅ |

---

## Shared Components

| Component | Status |
|:---|:---:|
| HeaderComponent | ✅ |
| FooterComponent | ✅ |
| LoadingSpinnerComponent | ✅ |
| ErrorDisplayComponent | ✅ |
| BreadcrumbComponent | ✅ |

---

## Guards & Interceptors

| Guard/Interceptor | Status |
|:---|:---:|
| AuthGuard | ✅ |
| AdminGuard | ✅ |
| SellerGuard | ✅ |
| AuthInterceptor (Cookie) | ✅ |
| ErrorInterceptor | ✅ |
