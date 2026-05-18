# Plan 15: E2E Test Infrastructure Remediation

## Goal
Fix all critical issues in the Playwright E2E test infrastructure: eliminate flaky waits, remove conditional-skip tests, improve POM locator quality, and establish proper auth fixtures.

## Context
- **Current state:** 21 test specs, 19 page objects (11 pages + 8 components), ~85% route coverage. Infrastructure is functional but has significant technical debt: 17 waitForTimeout calls, 30+ conditional-skip tests, 40+ duplicated login blocks, 40% fragile CSS selectors.
- **Target state:** All tests use proper waits, no conditional passes, auth fixture eliminates login duplication, all selectors use getByRole/getByTestId.
- **Root cause:** Tests were written incrementally without a shared auth fixture or locator strategy. Windows-only teardown and hardcoded URLs prevent CI portability.

## POM Coverage Matrix

| Route | Page Object | Test Spec | Status |
|---|---|---|---|
| /auth/login | LoginPage | login.spec.ts | COVERED |
| /auth/register | RegisterPage | (used in beforeEach) | COVERED |
| /auth/forgot-password | ForgotPasswordPage | NONE | GAP |
| /catalog | CatalogPage | browse-products.spec.ts | COVERED |
| /catalog/:id | ProductDetailPage+Enh | product-detail-enhanced | COVERED |
| /cart | CartPage | add-to-cart, cart-drawer | COVERED |
| /checkout | CheckoutPage+Enh | checkout-flow (x2) | COVERED |
| /orders | OrdersPage | order-history.spec.ts | COVERED |
| /orders/:id | OrderDetailPage+Enh | order-cancellation | COVERED |
| /profile | ProfileHubPage | profile, profile-hub | COVERED |
| /seller (dashboard) | SellerDashboardPage | seller-dashboard | COVERED |
| /seller/products | NONE | NONE | GAP |
| /seller/orders | SellerOrdersPage | seller-orders | COVERED |
| /seller/settings | StoreSettingsPage | seller-dashboard (partial) | COVERED |
| /admin | AdminPage | admin-panel | COVERED |
| /admin/store-verification | AdminStoreDetailPage | NONE | GAP |

Route coverage: ~85% (3 gaps: forgot-password, seller-products, admin store-detail)

## Task Plan

### Phase 1: Auth Fixture & Login Deduplication

- [ ] Create `fixtures/auth.fixture.ts` with `storageState`-based pre-authenticated contexts
- [ ] Add `buyerApi`, `sellerApi`, `adminApi` fixtures (merge from checkout/store fixtures)
- [ ] Replace 40+ duplicated login blocks across all specs with auth fixture
- [ ] Merge `store.fixture.ts` into `checkout.fixture.ts` or create shared `api-fixtures.ts`

**Files to create/modify:**
```
tests/E2ETests/fixtures/auth.fixture.ts              # NEW
tests/E2ETests/fixtures/checkout.fixture.ts           # Refactor
tests/E2ETests/fixtures/store.fixture.ts              # Remove or merge
tests/E2ETests/tests/**/*.spec.ts                     # All specs: use auth fixture
```

### Phase 2: Eliminate waitForTimeout

Replace all 17 `waitForTimeout` calls with proper waits:

| File | Line | Current | Replacement |
|---|---|---|---|
| browse-products.spec.ts | 40, 55 | waitForTimeout(500) | `await expect(locator).toBeVisible()` |
| add-to-cart.spec.ts | 27, 52 | waitForTimeout(500) | `await expect(cartBadge).toHaveText()` |
| checkout-flow.spec.ts | 40 | waitForTimeout(1000) | `await expect(page).toHaveURL(/checkout/)` |
| checkout-flow.spec.ts | 62 | waitForTimeout(500) | `await expect(saveBtn).toBeEnabled()` |
| checkout-flow.spec.ts | 78 | waitForTimeout(3000) | `await expect(orderConfirmed).toBeVisible()` |
| saga-cancellation.spec.ts | 45 | waitForTimeout(1000) | `await cartPage.waitForPageLoad()` |
| saga-cancellation.spec.ts | 63 | waitForTimeout(500) | `await expect(saveBtn).toBeEnabled()` |
| saga-cancellation.spec.ts | 72 | waitForTimeout(3000) | `await expect(statusCompleted).toBeVisible()` |
| saga-cancellation.spec.ts | 157, 224 | waitForTimeout(5000) | Polling helper with retry |
| saga-cancellation.spec.ts | 229 | waitForTimeout(2000) | `await page.waitForLoadState('networkidle')` |
| seller-order-correlation.spec.ts | 46 | waitForTimeout(3000) | Poll order status via API |
| cart-drawer.spec.ts | 45 | waitForTimeout(500) | `await expect(drawer).toBeVisible()` |
| header-mega-menu.spec.ts | 59 | waitForTimeout(500) | `await expect(menu).toBeVisible()` |
| inventory-management.spec.ts | 66, 72 | waitForTimeout(300) | `await expect(input).toHaveValue()` |
| product-detail-enhanced.spec.ts | 67 | waitForTimeout(500) | `await expect(page).toHaveURL()` |

**Files to modify:**
```
tests/E2ETests/tests/catalog/browse-products.spec.ts
tests/E2ETests/tests/cart/add-to-cart.spec.ts
tests/E2ETests/tests/checkout-flow.spec.ts
tests/E2ETests/tests/saga-aware-cancellation.spec.ts
tests/E2ETests/tests/seller-order-correlation.spec.ts
tests/E2ETests/tests/cart-drawer.spec.ts
tests/E2ETests/tests/header-mega-menu.spec.ts
tests/E2ETests/tests/inventory-management.spec.ts
tests/E2ETests/tests/product-detail-enhanced.spec.ts
```

### Phase 3: Remove Conditional-Skip Tests

Convert all `if (await el.isVisible())` patterns to either:
- API fixture guarantees data exists (preferred)
- `test.skip(!condition, 'reason')` with explicit reason
- `test.fixme()` for incomplete tests

Affected specs (30+ tests):
- order-cancellation.spec.ts (5 tests)
- product-detail-enhanced.spec.ts (6 tests)
- seller-orders.spec.ts (3 tests)
- inventory-management.spec.ts (3 tests)
- header-mega-menu.spec.ts (2 tests)
- browse-products.spec.ts (1 test)
- add-to-cart.spec.ts (2 tests)
- seller-order-correlation.spec.ts (1 test)

### Phase 4: Fix Fragile Locators

Replace CSS/Tailwind selectors with `data-testid` or semantic locators:

| Page Object | Current Selector | Replacement |
|---|---|---|
| store-settings.page.ts | `page.locator('input').first()` | `getByLabel()` or `getByTestId()` |
| admin.page.ts | `app-store-verification > div > div > div` | `getByTestId('store-list')` |
| mega-menu.component.ts | `.w-1\4 button` | `getByRole('button')` or `getByTestId()` |
| order-detail.page.ts | `div:has(> p.text-muted:text-is("Total"))` | `getByTestId('order-total')` |
| order-detail-enhanced.page.ts | 10 CSS selectors | `getByTestId()` for each |
| product-detail-enhanced.page.ts | 8 CSS selectors | `getByTestId()` for each |
| header.component.ts | `lucide-icon[name="ShoppingCart"]` | `getByTestId('cart-icon')` |
| cart.page.ts | Complex `hasNot` filter | `getByTestId('cart-item')` |

**Requires frontend team:** Add `data-testid` attributes to Angular templates.

**Files to modify:**
```
tests/E2ETests/pages/store-settings.page.ts
tests/E2ETests/pages/admin.page.ts
tests/E2ETests/pages/order-detail.page.ts
tests/E2ETests/pages/order-detail-enhanced.page.ts
tests/E2ETests/pages/product-detail-enhanced.page.ts
tests/E2ETests/components/header.component.ts
tests/E2ETests/components/mega-menu.component.ts
tests/E2ETests/pages/cart.page.ts
```

### Phase 5: Fix Type Safety & Weak Assertions

- [ ] Change `page: any` to `page: Page` in LoginPage, RegisterPage, ForgotPasswordPage, CatalogPage, ProfilePage
- [ ] Extract `fillStable()` to BasePage (deduplicate from LoginPage + RegisterPage)
- [ ] Fix weak assertions:
  - `expect(count).toBeGreaterThanOrEqual(0)` → assert exact count or > 0
  - `expect(typeof hasFBT).toBe('boolean')` → `expect(hasFBT).toBe(true)`
  - `expect(tableText).toBeTruthy()` → assert specific content
- [ ] Fix users.json: buyerUser should have distinct buyer credentials
- [ ] Fix admin password inconsistency (users.json vs payment-refund.spec.ts)

**Files to modify:**
```
tests/E2ETests/pages/login.page.ts
tests/E2ETests/pages/register.page.ts
tests/E2ETests/pages/forgot-password.page.ts
tests/E2ETests/pages/catalog.page.ts
tests/E2ETests/pages/profile.page.ts
tests/E2ETests/pages/base.page.ts
tests/E2ETests/data/users.json
tests/E2ETests/tests/payment-refund.spec.ts
```

### Phase 6: Config & Infrastructure

- [ ] Update `playwright.config.ts`:
  ```typescript
  timeout: 60000,
  expect: { timeout: 10000 },
  use: {
    actionTimeout: 15000,
    navigationTimeout: 30000,
    screenshot: 'on-first-retry',
    video: 'on-first-retry',
  },
  reporter: process.env.CI
    ? [['html'], ['junit', { outputFile: 'results.xml' }]]
    : 'html',
  ```
- [ ] Make `globalTeardown.ts` cross-platform (replace `taskkill` with `process.kill()`)
- [ ] Make `api-helpers.ts` baseUrl configurable (env var)
- [ ] Remove `db-helpers.ts` (dead code)
- [ ] Remove `console.log`/`console.error` from tests and helpers

**Files to modify:**
```
tests/E2ETests/playwright.config.ts
tests/E2ETests/globalTeardown.ts
tests/E2ETests/utils/api-helpers.ts
tests/E2ETests/utils/db-helpers.ts                  # Remove
```

### Phase 7: Missing Coverage

- [ ] Create `SellerProductsPage` page object
- [ ] Create `forgot-password.spec.ts` test
- [ ] Create `seller-products.spec.ts` test
- [ ] Create `admin-store-detail.spec.ts` test
- [ ] Consider adding `NotificationComponent` object (toast assertions)

**Files to create:**
```
tests/E2ETests/pages/seller-products.page.ts        # NEW
tests/E2ETests/tests/auth/forgot-password.spec.ts   # NEW
tests/E2ETests/tests/seller/seller-products.spec.ts # NEW
tests/E2ETests/tests/admin/admin-store-detail.spec.ts # NEW
tests/E2ETests/components/notification.component.ts # NEW
```

## Acceptance Criteria
- [ ] Zero `waitForTimeout` calls in any test file
- [ ] Zero conditional-skip tests (all tests either assert or skip explicitly)
- [ ] Auth fixture eliminates all duplicated login boilerplate
- [ ] All page objects use getByRole/getByTestId (zero CSS selectors)
- [ ] `playwright.config.ts` has explicit timeouts and retry artifacts
- [ ] `globalTeardown.ts` works on Linux (CI)
- [ ] `users.json` has distinct credentials per role
- [ ] All page constructors use `Page` type (not `any`)
- [ ] Route coverage: 100% (all Angular routes have at least one spec)
- [ ] `npx playwright test` passes with zero flaky retries

## Verification Commands
```bash
npx playwright test --reporter=list
npx playwright test --repeat-each=5 --reporter=list   # Flakiness check
npx playwright test --grep="@smoke" --reporter=list   # Smoke subset
```

## Decisions
| Decision | Rationale |
|----------|-----------|
| Auth fixture with storageState | Eliminates 40+ duplicated login blocks, matches Playwright best practice |
| data-testid over CSS | Stable across style changes, Angular team adds testid to templates |
| Polling over fixed waits | Handles variable latency (async saga, network) without flakiness |
| Keep Enhanced page objects | Avoid breaking existing tests; deprecate non-Enhanced over time |
| Keep db-helpers.ts removal | Dead code adds confusion, can be re-added when needed |
