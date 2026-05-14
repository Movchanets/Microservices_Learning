1. **CatalogService Tests (`catalog.service.spec.ts`)**:
   - create `src/web/src/app/features/catalog/catalog.service.spec.ts`
   - Test: `getProducts` correctly maps `ProductListParams` to `HttpParams`.
   - Test: `searchProducts` hits the `/api/search/` gateway endpoint.
2. **CatalogStore Tests (`catalog.store.spec.ts`)**:
   - create `src/web/src/app/features/catalog/catalog.store.spec.ts`
   - Test: `isSearchMode` computed signal (true when searchQuery is not empty).
   - Test: `totalPages` calculation based on `totalCount` and `pageSize`.
   - Test: `loadProducts` correctly updates `products` and `totalCount` state.
   - Test: `updateSearchQuery` resets page to 1.
3. **ProductListComponent Tests (`product-list.spec.ts`)**:
   - create `src/web/src/app/features/catalog/product-list/product-list.spec.ts`
   - Test: Renders product cards based on store state.
   - Test: Pagination triggers `goToPage` in the store.
4. **ProductCardComponent Tests (`product-card.spec.ts`)**:
   - create `src/web/src/app/features/catalog/components/product-card/product-card.spec.ts`
   - Test: Displays name, price (formatted), and "Add to Cart" button.
   - Test: Navigates to details on image click.
5. **Run tests**
   - Run tests: `cd src/web && pnpm run test --watch=false`
6. **Pre-commit step**:
   - Complete pre-commit steps to ensure proper testing, verification, review, and reflection are done.
