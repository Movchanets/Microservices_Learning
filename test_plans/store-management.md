# Test Plan: StoreManagement Service

## Current Coverage

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Unit | StoreTests, CreateStoreHandlerTests, GetStoreByIdHandlerTests, ListStoresHandlerTests, UpdateStoreHandlerTests, VerifySellerHandlerTests | ~25 | Covered |
| Integration | — | 0 | Not Covered |
| Contract | — | 0 | Not Covered |
| E2E | seller-dashboard.spec.ts | ~4 | Partially Covered |

## Test Scenarios — Unit

- [x] Store creation with valid data
- [x] Store name validation
- [x] CreateStoreCommand handler
- [x] GetStoreByIdQuery handler
- [x] ListStoresQuery handler
- [x] UpdateStoreCommand handler
- [x] VerifySellerCommand handler
- [ ] Store deactivation
- [ ] Store with multiple sellers (if supported)

## Test Scenarios — Integration

- [ ] StoreRepository CRUD
- [ ] Store → Catalog relationship (products belong to store)
- [ ] Store verification workflow

## Test Scenarios — E2E

- [x] Seller dashboard display
- [ ] Store settings CRUD (DELETED — was in store-settings-crud.spec.ts)
- [ ] Product CRUD (DELETED — was in seller-product-crud.spec.ts)
- [ ] Seller products list (DELETED — was in seller-products.spec.ts)
- [ ] Seller orders (DELETED — was in seller-orders.spec.ts)
- [ ] Seller order correlation (DELETED — was in seller-order-correlation.spec.ts)

## Gaps & Priority

| Gap | Priority | Notes |
|-----|----------|-------|
| 5 E2E spec files removed | P0 | seller-product-crud, store-settings-crud, seller-products, seller-orders, seller-order-correlation all deleted |
| Integration tests | P1 | No integration test project exists |
| Store deactivation unit test | P2 | Edge case |
