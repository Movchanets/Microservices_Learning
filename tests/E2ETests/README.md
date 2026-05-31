# E2E Test Infrastructure

Playwright-based end-to-end tests for the Marketplace application.

## Quick Start

```bash
# Run all tests
npx playwright test

# Run with UI
npx playwright test --ui

# Run specific suite
npx playwright test tests/auth/
npx playwright test tests/catalog/

# Run headed (watch browser)
npx playwright test --headed

# Generate Allure report
npx allure generate allure-results --clean -o allure-report
```

## Architecture

```
tests/E2ETests/
├── components/         # Component Objects (scoped UI fragments)
├── pages/              # Page Objects (full page abstractions)
├── fixtures/           # Playwright fixtures (test-base, auth)
├── utils/              # API helpers, constants, types
├── data/               # Test data (users.json)
├── test-data/          # Static assets (images)
├── tests/              # Test specs, organized by feature
└── docs/               # Test run results, reports
```

### Layers

| Layer | Purpose | Example |
|-------|---------|---------|
| **Components** | Scoped UI fragments (header, footer, cart drawer) | `HeaderComponent`, `CartDrawerComponent` |
| **Pages** | Full page abstractions with navigation + actions | `CatalogPage`, `ProductFormPage` |
| **Fixtures** | Dependency injection for pages/components/auth | `test-base.ts`, `auth.fixture.ts` |
| **Utils** | API helpers, constants, shared types | `store-helpers.ts`, `media-helpers.ts` |

### Key Patterns

#### Component Object Model (COM)

Every component extends `BaseComponent` and scopes locators to `this.root`:

```typescript
export class HeaderComponent extends BaseComponent {
  readonly logo: Locator;

  constructor(page: Page) {
    const root = page.locator('header');
    super(page, root);
    this.logo = this.root.getByTestId('header-logo'); // scoped!
  }
}
```

#### Page Object Model (POM)

Every page extends `BasePage` and inherits shared header/footer:

```typescript
export class CatalogPage extends BasePage {
  readonly searchInput: Locator;

  constructor(page: Page) {
    super(page);
    this.searchInput = page.getByTestId('search-input');
  }

  async search(query: string) {
    await this.searchInput.fill(query);
    await this.searchInput.press('Enter');
  }
}
```

#### Fixtures

`test-base.ts` provides all pages and components as fixtures:

```typescript
import { test, expect } from '../fixtures/test-base';

test('example', async ({ page, catalogPage, header }) => {
  // catalogPage and header are auto-instantiated
});
```

`auth.fixture.ts` adds pre-authenticated API + browser contexts:

```typescript
import { authTest as test, expect } from '../fixtures/auth.fixture';

test('auth example', async ({ sellerApi, buyerContext, adminApi }) => {
  // sellerApi = pre-authenticated API context
  // buyerContext = pre-authenticated browser context
  // adminApi = pre-authenticated admin API context
});
```

#### Angular-Safe Form Helpers

`BasePage` provides `fillStable()` and `submitWithRetry()` for Angular signal-based forms:

```typescript
// fillStable: retries up to 3 times to handle reactive form interference
await this.fillStable(this.nameInput, 'Product Name');

// submitWithRetry: fills fields + waits for button to be enabled
await this.submitWithRetry(this.submitBtn, [
  { input: this.emailInput, value: 'test@example.com' },
  { input: this.passwordInput, value: 'Password123!' },
]);
```

#### Timeout Constants

Always use `TIMEOUTS` from `utils/constants.ts` instead of magic numbers:

```typescript
import { TIMEOUTS } from '../utils/constants';

await expect(element).toBeVisible({ timeout: TIMEOUTS.element });
await page.waitForURL('**/path', { timeout: TIMEOUTS.api });
```

## API Helpers

| Helper | Purpose |
|--------|---------|
| `store-helpers.ts` | Create/verify stores, ensure store exists |
| `catalog-helpers.ts` | Create products, add SKUs, manage categories |
| `media-helpers.ts` | Upload images, manage galleries |
| `cart-helpers.ts` | Add/remove cart items |
| `order-helpers.ts` | Place orders, check status |
| `api-helpers.ts` | Login, get current user, generic API calls |
| `auth-helpers.ts` | Browser-level authentication flows |

## Writing Tests

### Naming Convention

```
{feature}/{feature-name}.spec.ts
```

Examples: `auth/login.spec.ts`, `catalog/browse-products.spec.ts`

### Test Structure

```typescript
test.describe('Feature: Sub-feature', () => {
  test('should do something when condition', async ({ page, somePage }) => {
    await test.step('Arrange: set up preconditions', async () => {
      // ...
    });

    await test.step('Act: perform the action', async () => {
      // ...
    });

    await test.step('Assert: verify the outcome', async () => {
      await expect(somePage.element).toBeVisible();
    });
  });
});
```

### Authenticated Tests

Use `authTest` for tests requiring authentication:

```typescript
import { authTest as test, expect } from '../../fixtures/auth.fixture';

test('seller action', async ({ sellerApi, sellerContext, adminApi }) => {
  // API calls via sellerApi
  // Browser interaction via sellerContext.newPage()
  // Admin operations via adminApi
});
```

## Known Limitations

- **Angular signal inputs**: `fillStable()` may not work with all signal-based inputs. Use `pressSequentially()` or `page.evaluate()` as fallback.
- **Skipped tests**: Tests marked with `test.skip` have known issues (usually Angular form compatibility). See inline TODO comments.
- **Storage state caching**: Auth fixture caches storage state for 30 min per worker. Delete `/tmp/playwright-auth/` to force re-login.
