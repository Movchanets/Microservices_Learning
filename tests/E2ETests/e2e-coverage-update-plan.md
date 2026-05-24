# E2E Test & POM Update Plan

## 1. POM Additions & Updates

### New Page Objects

- **`tests/E2ETests/pages/home.page.ts`** (Create)
  - **Missing Locators:** `heroBanner`, `categoryTiles`, `dealOfTheDay`, `featuredCarousel`, `newArrivalsCarousel`, `recentlyViewed`, `shopByCategoryHeading`
  - **Missing Methods:** `goto()`, `getCategoryTileCount()`, `clickCategoryTile(name)`, `getFeaturedProductCount()`, `addToCartFromCarousel(index)`

- **`tests/E2ETests/pages/store-page.page.ts`** (Create)
  - **Missing Locators:** `storeNameHeading`, `storeDescription`, `verifiedBadge`, `backToCatalogLink`, `productsGrid`, `productCards`, `emptyProductsMessage`, `loadingSkeleton`, `errorState`
  - **Missing Methods:** `goto(storeId)`, `getStoreName()`, `isVerified()`, `getProductCount()`, `clickProduct(index)`, `isLoading()`, `hasError()`

### New Component Models

- **`tests/E2ETests/components/toast-container.component.ts`** (Create)
  - **Missing Locators:** `container`, `toasts`, `successToasts`, `errorToasts`, `infoToasts`, `dismissBtns`
  - **Missing Methods:** `waitForToast(type)`, `getToastMessage(index)`, `dismissToast(index)`, `getToastCount()`, `expectSuccessToast(message)`, `expectErrorToast(message)`

- **`tests/E2ETests/components/not-found.component.ts`** (Create)
  - **Missing Locators:** `heading404`, `messageText`, `goHomeLink`
  - **Missing Methods:** `isVisible()`, `goHome()`

- **`tests/E2ETests/components/pagination.component.ts`** (Create)
  - **Missing Locators:** `container`, `prevBtn`, `nextBtn`, `pageButtons`, `ellipsis`
  - **Missing Methods:** `goToPage(n)`, `next()`, `previous()`, `getCurrentPage()`, `getTotalPages()`, `hasNext()`, `hasPrevious()`, `isVisible()`

- **`tests/E2ETests/components/category-sidebar.component.ts`** (Create)
  - **Missing Locators:** `container`, `allProductsBtn`, `categoryButtons`, `heading`
  - **Missing Methods:** `selectCategory(name)`, `selectAll()`, `getSelectedCategory()`, `getCategoryNames()`, `isVisible()`

### Updated Page Objects

- **`tests/E2ETests/pages/catalog.page.ts`** (Update)
  - **Missing Locators:** `sortDropdown`, `categorySidebar`, `searchFacets`, `pagination`, `priceMinInput`, `priceMaxInput`, `inStockCheckbox`, `productCount`, `emptyState`, `loadingSkeleton`
  - **Missing Methods:** `sortBy(option)`, `filterByCategory(name)`, `setPriceRange(min, max)`, `toggleInStockOnly()`, `goToPage(n)`, `getProductCount()`, `isLoading()`, `isEmpty()`

- **`tests/E2ETests/pages/seller-products.page.ts`** (Update)
  - **Missing Locators:** `productNameColumn`, `productStatusColumn`, `editBtns`, `deleteBtns`, `confirmDeleteBtn`, `searchInput`, `statusFilterSelect`
  - **Missing Methods:** `clickAddProduct()`, `editProduct(index)`, `deleteProduct(index)`, `confirmDelete()`, `searchProducts(query)`, `getProductName(index)`, `getProductStatus(index)`, `filterByStatus(status)`

- **`tests/E2ETests/pages/store-settings.page.ts`** (Update)
  - **Missing Locators:** `successMessage`, `errorMessage`, `loadingSpinner`
  - **Missing Methods:** `fillStoreForm(name, description, email)`, `expectSuccess()`, `expectError(message)`, `isLoading()`

- **`tests/E2ETests/pages/admin.page.ts`** (Update)
  - **Missing Locators:** `searchInput`, `noUsersMessage`, `userCount`, `deactivateConfirmDialog`
  - **Missing Methods:** `searchUsers(query)`, `getUserCount()`, `confirmDeactivation()`

- **`tests/E2ETests/pages/profile-hub.page.ts`** (Update)
  - **Missing Locators:** `successMessage`, `errorMessage`, `passwordValidationErrors`
  - **Missing Methods:** `expectProfileUpdateSuccess()`, `expectPasswordChangeSuccess()`, `expectValidationError(message)`

## 2. Test Coverage Additions

### New Test Specs

- **`tests/E2ETests/tests/home/home-page.spec.ts`** (Create)
  - **Target Feature:** Home Page / Landing
  - **Scenarios:**
    - Display hero banner and category tiles on load
    - Navigate to catalog when clicking a category tile
    - Display featured products carousel
    - Add product to cart from featured carousel
    - Show recently viewed section after browsing products

- **`tests/E2ETests/tests/catalog/catalog-filter-sort.spec.ts`** (Create)
  - **Target Feature:** Catalog Filtering, Sorting, Pagination
  - **Scenarios:**
    - Filter products by category via sidebar
    - Filter products by price range
    - Sort products by price ascending/descending
    - Paginate through product pages
    - Toggle in-stock-only filter
    - Combine multiple filters (category + price + in-stock)

- **`tests/E2ETests/tests/seller/seller-product-crud.spec.ts`** (Create)
  - **Target Feature:** Seller Product CRUD
  - **Scenarios:**
    - Create a new product with valid form data
    - Edit an existing product's name and description
    - Delete a product after confirmation
    - Show validation errors for empty required fields
    - Navigate between product list and product form

- **`tests/E2ETests/tests/seller/store-settings-crud.spec.ts`** (Create)
  - **Target Feature:** Store Settings Management
  - **Scenarios:**
    - Display store settings page for seller
    - Update store name and description
    - Show success toast after saving changes
    - Show validation error for empty store name

- **`tests/E2ETests/tests/admin/admin-user-management.spec.ts`** (Create)
  - **Target Feature:** Admin User Management
  - **Scenarios:**
    - Display user list with roles and join dates
    - Change a user's role from Buyer to Seller
    - Deactivate a user with confirmation dialog
    - Verify deactivated user cannot log in

- **`tests/E2ETests/tests/profile/profile-settings.spec.ts`** (Create)
  - **Target Feature:** Profile Settings & Password Change
  - **Scenarios:**
    - Display current user profile information
    - Update first name and last name successfully
    - Change password with valid current password
    - Show error for wrong current password
    - Show validation error for mismatched new password confirmation

- **`tests/E2ETests/tests/checkout/checkout-edge-cases.spec.ts`** (Create)
  - **Target Feature:** Checkout Edge Cases
  - **Scenarios:**
    - Redirect to login when accessing checkout unauthenticated
    - Show empty cart message when no items in cart
    - Validate required address fields before proceeding
    - Select express shipping and verify cost change

- **`tests/E2ETests/tests/not-found.spec.ts`** (Create)
  - **Target Feature:** 404 Not Found Page
  - **Scenarios:**
    - Display 404 heading and message for unknown routes
    - Navigate home via "Go Home" link

## 3. Execution Strategy & Refactoring

### DRY Improvements

1. **Extract register-and-login helper** — 8+ test files duplicate the `registerPage.goto → register → waitForRedirect → conditional login` pattern. Extract to a `registerAndLogin(page, registerPage, loginPage)` helper in `fixtures/`.

2. **Unify checkout setup** — `checkout-flow.spec.ts`, `saga-aware-cancellation.spec.ts`, and `seller-order-correlation.spec.ts` all duplicate the register → add-to-cart → fill-address → select-shipping → place-order flow. Extract to a `completeCheckoutFlow(page, buyerApi, ...)` helper.

3. **Move page-object instantiation to fixtures** — `admin-store-detail.spec.ts` and `seller-products.spec.ts` manually instantiate POMs (`new AdminStoreDetailPage(page)`) instead of using fixtures. Wire them through `test-base.ts`.

### Flaky Test Patterns to Fix

1. **`waitForTimeout` in login/register retry loops** — The 100ms/150ms `waitForTimeout` in `fillStable()` and login/register POMs can cause flakiness on slow CI. Replace with `expect(input).toHaveValue(value, { timeout: 2000 })` which polls automatically.

2. **`page.waitForLoadState('domcontentloaded')` after click** — Many tests use this as a generic "wait for navigation" but it fires immediately if the page was already loaded. Use `page.waitForURL()` or `expect(locator).toBeVisible()` instead for deterministic waits.

3. **`isVisible()` used as boolean assertion** — Tests like `expect(await foo.isVisible()).toBe(true)` are flaky because `isVisible()` returns immediately. Use `await expect(foo).toBeVisible()` which retries with timeout.

4. **Category filter test skips silently** — `browse-products.spec.ts` line 54 uses `test.skip(true, ...)` when no category buttons found. This masks failures. The seeder should guarantee categories exist.

5. **`page.once('dialog', ...)` race condition** — `admin.page.ts` registers the dialog handler *after* clicking the button. On fast browsers the dialog fires before the handler. Register the handler before the click using `page.on('dialog', ...)`.
