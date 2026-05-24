# Task: Implement Catalog Feature Unit Tests (Frontend)

**Goal**: Implement comprehensive unit tests for the Catalog feature, focusing on the complex NgRx SignalStore logic and data fetching.

**Context**: 
- Framework: Angular 21
- Testing: Vitest
- Location: `src/web/src/app/features/catalog/`
- Reference Plans: `7.2.1` through `7.2.5`

**Action Items**:
1. **CatalogService Tests (`catalog.service.spec.ts`)**:
   - Test: `getProducts` correctly maps `ProductListParams` to `HttpParams`.
   - Test: `searchProducts` hits the `/api/search/` gateway endpoint.
2. **CatalogStore Tests (`catalog.store.spec.ts`)**:
   - Test: `isSearchMode` computed signal (true when searchQuery is not empty).
   - Test: `totalPages` calculation based on `totalCount` and `pageSize`.
   - Test: `loadProducts` correctly updates `products` and `totalCount` state.
   - Test: `updateSearchQuery` resets page to 1.
3. **ProductListComponent Tests (`product-list.spec.ts`)**:
   - Test: Renders product cards based on store state.
   - Test: Pagination triggers `goToPage` in the store.
4. **ProductCardComponent Tests (`product-card.spec.ts`)**:
   - Test: Displays name, price (formatted), and "Add to Cart" button.
   - Test: Navigates to details on image click.

**Validation**:
- Run: `cd src/web && pnpm run test --watch=false`
