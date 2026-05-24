# Task Plan: Store Dashboard — Store Creation + Product Management

## Goal
Build a complete Store Dashboard on the Angular frontend where sellers can create their store, create products (with category, tags, image), manage product lifecycle (activate/deactivate/edit/delete), and view inventory — all wired to the existing StoreManagement and Catalog backend APIs.

## Current Phase
Phase 7 (all complete)

## Phases

### Phase 1: Research & Gap Analysis
- [x] Map StoreManagement backend (Domain, API, Application)
- [x] Map Catalog backend (Product aggregate, endpoints, commands)
- [x] Map frontend seller-dashboard (routes, stores, services, components)
- [x] Identify gaps between backend capabilities and frontend UI
- **Status:** complete

### Phase 2: Backend — DeactivateProduct Endpoint
- [x] `GET /api/catalog/categories` already existed
- [x] Added `DeactivateProductCommand` + handler
- [x] Added `PUT /{id}/deactivate` endpoint
- **Status:** complete

### Phase 3: Frontend — Store Creation Flow
- [x] Dashboard shows welcome screen when no store
- [x] Store creation form with name + description
- [x] StoreId persisted to localStorage on create/load
- [x] Dashboard shows tabs + router-outlet after store exists
- **Status:** complete

### Phase 4: Frontend — Product Form Enhancements
- [x] CategoryService created, category dropdown in form
- [x] Tags input (comma-separated)
- [x] Image URL field with preview
- [x] All fields wired to Create/Update requests
- [x] Edit mode populates form via effect() on selectedProduct
- [x] Price changes use separate ChangePrice endpoint
- **Status:** complete

### Phase 5: Frontend — Product Lifecycle Actions
- [x] Activate/deactivate buttons on product list
- [x] Wired to activate + deactivate endpoints
- [x] Status badges with color coding
- [x] Toast notifications on actions
- **Status:** complete

### Phase 6: Frontend — Store Settings Improvements
- [x] Creation date + verified date displayed
- [x] Verification status banner with icon + message + rejection reason
- [x] Logo URL input with preview
- [x] setLogo method in store + service
- **Status:** complete

### Phase 7: Testing & Verification
- [x] Backend builds with 0 errors
- [x] All 70 seller-dashboard tests pass
- [x] 11 pre-existing failures in orders/cart (not related)
- **Status:** complete

## Key Questions
1. **Category selection**: Backend has Category entity but no public list endpoint. Need to add one?
   → YES. Add `GET /api/catalog/categories` to Catalog API.
2. **Product deactivation**: Backend has `Deactivate()` on Product but no endpoint/command. Need to add?
   → YES. Add `DeactivateProductCommand` + `PUT /{id}/deactivate` endpoint.
3. **StoreId type mismatch**: Store.Id is Guid, Product.StoreId is Guid. SellerId is string (Identity). No issue — storeId flows correctly.
4. **Image upload**: Media service exists? Need to check. For now, use image URL field.
   → Deferred. Use URL input for now, file upload can be Phase 2.

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Add GET /categories endpoint | Product form needs category dropdown; no public endpoint exists |
| Add DeactivateProduct command | Product.Deactivate() exists in domain but no API endpoint |
| Use URL for images (not upload) | Media service integration is separate; URL field is MVP |
| Keep storeId in localStorage | Already used by SellerProductStore and InventoryStore; consistent |
| Category dropdown in product form | Required for CreateProductCommand.CategoryId |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| (none yet) | | |

## Notes
- Backend is largely complete. Gaps: category list endpoint, deactivate product endpoint.
- Frontend has skeleton but product form is incomplete (missing category, tags, image).
- Store creation exists in settings but dashboard page doesn't handle "no store" state.
- SellerProductStore.loadProducts reads storeId from localStorage — must be set after store creation.
