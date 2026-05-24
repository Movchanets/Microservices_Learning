# Findings & Decisions — Store Dashboard

## Requirements
- Seller can create a store (name + description)
- Seller can create products with full details (name, SKU, description, price, category, tags, image URL)
- Seller can manage products (edit, activate, deactivate, delete)
- Seller can view inventory levels
- Seller can manage store settings

## Research Findings

### Backend — StoreManagement API (COMPLETE)
- **Store aggregate**: Id (Guid), SellerId (string), Name, Description, LogoUrl, VerificationStatus (Pending/Verified/Rejected)
- **Endpoints**: POST /, GET /, GET /{id}, GET /seller/{sellerId}, PUT /{id}, POST /{id}/verify, PUT /{id}/logo
- **Commands**: CreateStore, UpdateStore, VerifySeller, SetStoreLogo
- **Queries**: ListStores, GetStoreById, GetStoreBySellerId
- CreateStore checks for duplicate sellerId → one store per seller

### Backend — Catalog API (MOSTLY COMPLETE)
- **Product aggregate**: Id, Name, Description, Price (Money VO), Sku (VO), CategoryId, StoreId (Guid), Status (Draft/Active/Inactive/Deleted), ImageUrl, Tags[]
- **Endpoints**: Full CRUD + featured, by-ids, sku lookup, recommendations, reviews
- **Commands**: CreateProduct, UpdateProduct, DeleteProduct (soft), ChangePrice, ActivateProduct
- **MISSING**: DeactivateProduct command + endpoint (Product.Deactivate() exists in domain)
- **MISSING**: Public GET /categories endpoint (Category entity exists, CRUD commands exist, but no public list)
- ListProducts supports `?storeId=` filter — used by seller dashboard

### Backend — Catalog Category
- Category entity has: Id, Name, Description, ParentCategoryId, Slug, SortOrder, IsActive
- CRUD commands exist (CreateCategory, UpdateCategory, DeleteCategory)
- No public list endpoint for categories

### Frontend — Seller Dashboard (PARTIAL)
- **Route**: `/seller` — guarded by authGuard + roleGuard('Seller', 'Admin')
- **Layout**: Dashboard page with tabs (Products, Orders, Inventory, Settings) + RouterOutlet
- **SellerProductStore**: loads products by storeId from localStorage, CRUD operations
- **SellerProductService**: calls `/api/catalog/products?storeId=...`
- **StoreSettingsStore**: load by sellerId, createStore, updateSettings
- **StoreService**: calls `/api/stores` endpoints
- **InventoryStore**: joins products + inventory by SKU, add stock

### Frontend — Product Form Gaps
- Missing: category dropdown (categoryId is hardcoded to '')
- Missing: tags input
- Missing: image URL field
- Missing: product data population when editing (loadProductById is called but form fields aren't populated from selectedProduct)
- Price/stock fields exist but stock is Inventory concern, not Catalog

### Frontend — Store Creation Gap
- Store creation exists in store-settings component (when no store)
- Dashboard page doesn't check for store existence — loads sales summary regardless
- No guided "create your store first" flow on dashboard entry

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| Add `GET /api/catalog/categories` endpoint | Frontend needs category list for product form dropdown |
| Add `DeactivateProductCommand` + endpoint | Domain method exists, need API exposure |
| Use signal for category list in product form | Simple fetch, no need for full SignalStore |
| Populate form fields via effect() on selectedProduct | Matches existing store-settings pattern |
| Keep storeId in localStorage | Already used by 2 stores; changing approach = larger refactor |

## Issues Encountered
| Issue | Resolution |
|-------|------------|
| (none yet) | |

## Resources
- StoreManagement endpoints: `src/Microservices/StoreManagement/StoreManagement.API/Endpoints/StoreEndpoints.cs`
- Catalog endpoints: `src/Microservices/Catalog/Catalog.API/Endpoints/ProductEndpoints.cs`
- Seller routes: `src/web/src/app/features/seller-dashboard/seller.routes.ts`
- Product form: `src/web/src/app/features/seller-dashboard/product-form/product-form.ts`
- Store settings: `src/web/src/app/features/seller-dashboard/store-settings/store-settings.ts`
