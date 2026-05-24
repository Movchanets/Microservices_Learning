# Test Plan: Catalog Service

## Current Coverage

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Unit | ProductTests, CategoryTests, CreateProductHandlerTests, DeleteProductHandlerTests, GetCategoriesHandlerTests, GetProductRecommendationsHandlerTests, GetProductsQueryHandlerTests, UpdateProductPriceHandlerTests | ~45 | Covered |
| Integration | ProductRepositoryTests, CategoryRepositoryTests, OutboxIntegrationTests | ~15 | Partially Covered |
| Contract | CatalogToCartContractTests, CatalogToInventoryContractTests, CatalogToSearchContractTests | ~15 | Covered |
| E2E | browse-products.spec.ts, catalog-filter-sort.spec.ts | ~10 | Partially Covered |

## Test Scenarios — Unit

- [x] Product creation with valid data
- [x] Product name validation (empty, too long)
- [x] Price must be positive
- [x] Category creation and hierarchy
- [x] CreateProductCommand handler
- [x] DeleteProductCommand handler
- [x] GetProductsQuery with filters
- [x] GetProductRecommendations handler
- [x] UpdateProductPrice handler
- [ ] UpdateProductCommand handler (NOT price — separate endpoint)

## Test Scenarios — Integration

- [x] ProductRepository CRUD
- [x] CategoryRepository CRUD
- [x] Outbox publishes integration events
- [ ] Product search after indexing (cross-service)
- [ ] Category with products cascade delete

## Test Scenarios — E2E

- [x] Browse products grid
- [x] Filter by category
- [x] Sort by price/name
- [x] Search by keyword
- [x] Empty state for no results
- [ ] Write review (DELETED — re-add)
- [ ] Product detail page error state
- [ ] Search facets interaction
- [ ] Price change via separate PATCH endpoint (catalog API constraint)

## Gaps & Priority

| Gap | Priority | Notes |
|-----|----------|-------|
| UpdateProductCommand (no price) | P1 | Verify price changes go through PATCH /{id}/price |
| Write review E2E | P2 | Review submission, validation, character limits |
| Search facets interaction E2E | P2 | Facet clicks update results |
