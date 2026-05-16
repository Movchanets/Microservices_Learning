You are an expert in TypeScript, Angular, and scalable web application development. You write functional, maintainable, performant, and accessible code following Angular and TypeScript best practices.

## Project Overview

Angular **v21** e-commerce frontend (`web-frontend`) with SSR via `@angular/ssr`, **NgRx SignalStore** for state management, **Tailwind CSS v4** + **Spartan UI** for styling, **Lucide** for icons, and **Vitest** as the test runner. Package manager: **pnpm**.

---

## Project Structure

```
src/app/
├── core/                      # Singleton services, guards, interceptors
│   ├── auth/                  # AuthStore, AuthService, authGuard, roleGuard
│   ├── http/                  # api.interceptor, error.interceptor
│   ├── services/              # toast, category-tree, inventory
│   ├── signalr/               # NotificationService, NotificationBridgeComponent
│   ├── language.service.ts
│   └── theme.service.ts
├── features/                  # Lazy-loaded feature modules
│   ├── auth/                  # login, register, forgot-password, profile/
│   ├── catalog/               # product-list, product-detail, components/
│   │   ├── catalog.store.ts   # Feature-scoped (NOT providedIn: 'root')
│   │   ├── catalog.service.ts
│   │   ├── catalog.models.ts
│   │   ├── catalog.routes.ts
│   │   └── components/        # product-card, buy-box, frequently-bought-together,
│   │                          # category-sidebar, pagination, search-facets
│   ├── cart/                  # cart-page, mini-cart, CartStore
│   ├── checkout/              # checkout-page, checkout-status, checkout-summary, address-form
│   ├── orders/                # order-list, order-detail, order-timeline, status-badge
│   ├── seller-dashboard/      # dashboard-page, product-list, product-form, seller-orders, store-settings
│   └── admin/                 # admin-page, user-list, store-verification, store-detail
├── shared/                    # Reusable components and pages
│   ├── components/            # header, footer, mega-menu, cart-drawer, toast-container, stock-indicator
│   └── pages/                 # not-found
├── app.ts                     # Root component
├── app.config.ts              # Client-side providers (router, HttpClient, Lucide, interceptors, initializers)
├── app.config.server.ts       # Server-side providers (SSR)
├── app.routes.ts              # Root routing table
└── app.routes.server.ts       # SSR render mode config
```

---

## NgRx SignalStore Patterns

9 stores total. **8 are `providedIn: 'root'` singletons; `CatalogStore` is feature-scoped.**

| Store | Scope | Key State |
|---|---|---|
| `AuthStore` | root | user, loading, error |
| `CartStore` | root | items, loading, error, checkoutCorrelationId, isDrawerOpen |
| `CatalogStore` | **feature** | products, categories, facets, pagination, filters, loading, error |
| `CheckoutStore` | root | address, shippingMethod, submitting, error, order |
| `OrderStore` | root | orders, selectedOrder, loading, error |
| `AdminStore` | root | users, stores, pendingStores, selectedStore, loading, error |
| `SellerProductStore` | root | products, selectedProduct, loading, error |
| `StoreSettingsStore` | root | settings, salesSummary, loading, error |
| `ProfileStore` | root | updating, changingPassword, error, successMessage |

### Store conventions

```typescript
export const SomeStore = signalStore(
  { providedIn: 'root' },  // or omit for feature-scoped
  withState<SomeState>({ ... }),
  withComputed((store) => ({
    derivedValue: computed(() => ...),
  })),
  withMethods((store) => ({
    async loadSomething(): Promise<void> {
      patchState(store, { loading: true });
      try {
        const data = await someService.fetch();
        patchState(store, { data, loading: false });
      } catch (err: unknown) {
        patchState(store, { error: 'Failed', loading: false });
      }
    },
  })),
);
```

Rules:
- Use `patchState` / `set` / `update` — **never** `mutate`
- Use `inject()` at the class field level, **not** inside `withMethods` body
- Use `computed()` for derived state
- All async methods must handle loading + error states

---

## Routing

All feature routes are lazy-loaded via `loadComponent`:

```typescript
// catalog.routes.ts — named export
export const CATALOG_ROUTES: Routes = [...];

// cart.routes.ts — default export
export default [...];
```

Guards: `authGuard` (functional `CanActivateFn`), `roleGuard('Seller', 'Admin')` (factory returning `CanActivateFn`). Both skip SSR.

SSR render modes (in `app.routes.server.ts`): `RenderMode.Server` for catalog, checkout, orders, seller, admin; `RenderMode.Prerender` for everything else.

---

## Angular Best Practices

- Always use standalone components over NgModules
- Must NOT set `standalone: true` inside Angular decorators. It's the default in Angular v20+.
- Use signals for state management
- Implement lazy loading for feature routes
- Do NOT use the `@HostBinding` and `@HostListener` decorators. Put host bindings inside the `host` object of the `@Component` or `@Directive` decorator instead
- Use `NgOptimizedImage` for all static images.
  - `NgOptimizedImage` does not work for inline base64 images.

## TypeScript Best Practices

- Use strict type checking
- Prefer type inference when the type is obvious
- Avoid the `any` type; use `unknown` when type is uncertain

## Accessibility Requirements

- It MUST pass all AXE checks.
- It MUST follow all WCAG AA minimums, including focus management, color contrast, and ARIA attributes.

### Components

- Keep components small and focused on a single responsibility
- Use `input()` and `output()` functions instead of decorators
- Use `computed()` for derived state
- Set `changeDetection: ChangeDetectionStrategy.OnPush` in `@Component` decorator
- Prefer inline templates for small components
- Prefer Reactive forms instead of Template-driven ones
- Do NOT use `ngClass`, use `class` bindings instead
- Do NOT use `ngStyle`, use `style` bindings instead
- When using external templates/styles, use paths relative to the component TS file.

## State Management

- Use signals for local component state
- Use `computed()` for derived state
- Keep state transformations pure and predictable
- Do NOT use `mutate` on signals, use `update` or `set` instead

## Templates

- Keep templates simple and avoid complex logic
- Use native control flow (`@if`, `@for`, `@switch`) instead of `*ngIf`, `*ngFor`, `*ngSwitch`
- Use the async pipe to handle observables
- Do not assume globals like (`new Date()`) are available.

## Services

- Design services around a single responsibility
- Use the `providedIn: 'root'` option for singleton services
- Use the `inject()` function instead of constructor injection
- Guard browser-only APIs (localStorage, window) with `isPlatformBrowser` checks

---

## HTTP & API Integration

All API calls go through the YARP BFF gateway:

```typescript
// All requests include withCredentials: true for cookie-based auth
@Injectable({ providedIn: 'root' })
export class SomeService {
  private http = inject(HttpClient);

  getSomething(): Promise<SomeType> {
    return firstValueFrom(this.http.get<SomeType>('/api/some-endpoint'));
  }
}
```

Interceptors (functional `HttpInterceptorFn`):
- `api.interceptor` — adds `withCredentials: true`
- `error.interceptor` — global error handling

---

## SSR Considerations

- Guards (`authGuard`, `roleGuard`) skip execution during SSR
- `CartStore.onInit` only loads cart in browser (`isPlatformBrowser` check)
- Browser-only APIs must be guarded with `isPlatformBrowser`
- `app.routes.server.ts` controls per-route render mode

---

## Testing

- **Runner**: Vitest (not Karma)
- **Command**: `npx ng test` (or `pnpm test` which runs `ng test --watch=false`)
- **Pattern**: `*.spec.ts` files co-located with source files
- **30 spec files**, **170 tests** passing
- Use `TestBed.configureTestingModule` with `imports: [ComponentUnderTest]`
- Mock services via `{ provide: ServiceClass, useValue: mockObject }`
- Lucide icons in tests: `providers: [importProvidersFrom(LucideAngularModule.pick({ Icon1, Icon2 }))]`
- Set inputs via `fixture.componentRef.setInput('inputName', value)`

### Test gaps (known)
- Admin feature: 0 tests
- Profile store: 0 tests
- Catalog store/service: 0 tests
- Seller dashboard components: 0 tests
