# Marketplace Frontend

## Overview

The Marketplace Angular SPA is a server-side rendered (SSR) application built with Angular 21, standalone components, signals, and NgRx SignalStore. It communicates with backend microservices exclusively through the YARP API Gateway using the BFF (Backend-for-Frontend) pattern.

## Tech Stack

| Layer | Technology | Version | Why |
|-------|-----------|---------|-----|
| **Framework** | Angular | 21+ | Standalone components, signals, SSR hydration, zoneless-ready |
| **State** | NgRx SignalStore | 21+ | Signal-based reactivity without RxJS boilerplate |
| **CSS** | Tailwind CSS | v4 | Utility-first, `@theme` directive for design tokens |
| **UI Primitives** | @spartan-ng | 0.0.1-alpha | Headless, accessible components (Dialog, Tabs, Select) |
| **Icons** | Lucide Angular | 1.0+ | Consistent SVG icon set, tree-shakeable |
| **HTTP** | Angular HttpClient | Built-in | Fetch API adapter, interceptor chain, XSRF support |
| **Testing** | Vitest | 4.0+ | Fast, Vite-native test runner |
| **Build** | Angular CLI + esbuild | 21+ | Fast builds, code splitting, SSR support |
| **Package Manager** | pnpm | 10.0 | Fast, disk-efficient, strict dependency resolution |

## Architecture

```
src/web/src/app/
├── core/                    # Singleton services, guards, interceptors
│   ├── auth/                # AuthStore, auth guard
│   ├── http/                # API interceptor (withCredentials, error handling)
│   └── signalr/             # NotificationService (stubbed, Phase 5)
├── features/                # Lazy-loaded feature modules
│   ├── auth/                # Login, register, forgot-password, profile
│   ├── catalog/             # Product list, detail, search, filters
│   ├── cart/                # Cart page, mini-cart, cart store
│   ├── checkout/            # Checkout flow, order status tracking
│   ├── orders/              # Order history, order detail
│   └── seller-dashboard/    # Product management (Phase 7.5)
├── shared/                  # Reusable components, pipes, directives
├── app.component.ts         # Root component with nav shell
├── app.config.ts            # Provider configuration
├── app.routes.ts            # Route definitions
└── app.routes.server.ts     # SSR render mode configuration
```

## Key Patterns

### Standalone Components (No NgModules)

Every component is `standalone: true`. No NgModules anywhere. This enables tree-shaking and simpler dependency graphs.

```typescript
@Component({
  selector: 'app-example',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, LucideAngularModule],
  template: `...`
})
export class ExampleComponent {}
```

**Why**: Standalone components are the Angular standard since v15. They eliminate NgModule boilerplate and improve tree-shaking.

### Signals-First State Management

Local state uses Angular signals. Global state uses NgRx SignalStore. No RxJS subscriptions for component state.

```typescript
// Local state
const count = signal(0);
const doubled = computed(() => count() * 2);

// Global state (SignalStore)
export const CartStore = signalStore(
  { providedIn: 'root' },
  withState<CartState>({ items: [], loading: false }),
  withComputed((store) => ({
    totalItems: computed(() => store.items().reduce((sum, i) => sum + i.quantity, 0)),
  })),
  withMethods((store, cartService = inject(CartService)) => ({
    async loadCart(): Promise<void> {
      patchState(store, { loading: true });
      const items = await cartService.getCart();
      patchState(store, { items, loading: false });
    },
  }))
);
```

**Why**: Signals provide fine-grained reactivity without zone.js. SignalStore combines the ergonomics of NgRx with the simplicity of signals. No actions, reducers, or effects — just methods.

### Change Detection Strategy: OnPush

Every component uses `ChangeDetectionStrategy.OnPush`. Combined with signals, this ensures Angular only re-renders when signal values actually change.

**Why**: OnPush + signals eliminates unnecessary change detection cycles. This is the path to zoneless Angular.

### Lazy Loading with `loadComponent`

All feature routes use lazy loading via `loadComponent` or `loadChildren`. No eagerly loaded feature modules.

```typescript
export const routes: Routes = [
  { path: 'catalog', loadChildren: () => import('./features/catalog/catalog.routes') },
  { path: 'orders', loadChildren: () => import('./features/orders/orders.routes') },
];
```

**Why**: Each feature loads only when navigated to. This keeps the initial bundle small.

### BFF Pattern (All API Calls Through Gateway)

All HTTP calls go through the YARP API Gateway at `/api/*`. No direct microservice URLs.

```typescript
// cart.service.ts
private readonly baseUrl = '/api/cart';

async getCart(): Promise<ShoppingCart> {
  return firstValueFrom(
    this.http.get<ShoppingCart>(this.baseUrl, { headers: this.getHeaders() })
  );
}
```

**Why**: The gateway handles cookie-to-bearer token exchange, CSRF protection, and service discovery. The frontend never knows about internal service URLs.

### API Interceptor

The `apiInterceptor` adds `withCredentials: true` to all requests so session cookies are sent.

```typescript
export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const apiReq = req.clone({ withCredentials: true });
  return next(apiReq);
};
```

**Why**: The BFF uses encrypted session cookies (not JWT in localStorage) for security. `withCredentials` ensures cookies are sent cross-origin.

### XSRF Protection

Configured in `app.config.ts` with double-submit cookie pattern.

```typescript
provideHttpClient(
  withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' }),
)
```

**Why**: Prevents cross-site request forgery attacks on state-changing requests (POST, PUT, DELETE).

### New Control Flow Syntax

All templates use Angular's built-in `@if`, `@for`, `@switch` — never `*ngIf`, `*ngFor`.

```html
@if (loading()) {
  <app-skeleton />
} @else {
  @for (item of items(); track item.id) {
    <app-item-card [item]="item" />
  } @empty {
    <p>No items found</p>
  }
}
```

**Why**: Built-in control flow is faster (no directive overhead), type-safe, and the Angular standard since v17.

### Template Methods -> Computed Signals

Never call methods in templates. Use `computed()` for derived values.

```typescript
// BAD — called on every change detection cycle
getTotal(): number { return this.items.reduce((s, i) => s + i.price, 0); }

// GOOD — only recomputes when items signal changes
total = computed(() => this.items().reduce((s, i) => s + i.price, 0));
```

**Why**: Template methods execute on every change detection cycle. Computed signals cache their value and only recompute when dependencies change.

### SSR with Render Modes

The app supports server-side rendering with per-route render mode configuration.

```typescript
// app.routes.server.ts
export const serverRoutes: ServerRoute[] = [
  { path: 'catalog', renderMode: RenderMode.Server },
  { path: 'catalog/:id', renderMode: RenderMode.Server },
  { path: 'checkout', renderMode: RenderMode.Server },
  { path: 'orders', renderMode: RenderMode.Server },
  { path: 'orders/:id', renderMode: RenderMode.Server },
  { path: '**', renderMode: RenderMode.Prerender },
];
```

**Why**: SSR improves initial load performance and SEO. Dynamic routes (with `:id` params) use `RenderMode.Server` to avoid prerender errors. Static routes use `RenderMode.Prerender`.

### Client Hydration

Configured with `provideClientHydration(withEventReplay())`.

**Why**: Hydration makes the server-rendered HTML interactive without re-rendering. `withEventReplay()` queues browser events that fire before hydration and replays them after.

## HTTP Communication

### Request Flow

```
Angular Component
  -> SignalStore method
    -> Service (HttpClient)
      -> apiInterceptor (adds withCredentials)
        -> XSRF interceptor (adds X-XSRF-TOKEN header)
          -> YARP Gateway (/api/*)
            -> Microservice
```

### Buyer Identification

The buyer ID is stored in `localStorage` and sent via `x-buyer-id` header on cart requests.

```typescript
private getHeaders(): HttpHeaders {
  const buyerId = localStorage.getItem('buyerId') || 'guest-user';
  return new HttpHeaders({ 'x-buyer-id': buyerId });
}
```

**Why**: The BFF extracts the buyer ID from the JWT claims and sets it as a header. For development, we use localStorage as a stand-in.

## State Management Architecture

### Feature Store Pattern

Each feature has its own SignalStore. Stores are `providedIn: 'root'` for singleton access.

| Store | Feature | State |
|-------|---------|-------|
| `AuthStore` | Auth | user, isAuthenticated, loading |
| `CartStore` | Cart | items, loading, checkoutCorrelationId |
| `CatalogStore` | Catalog | products, categories, filters, loading |
| `CheckoutStore` | Checkout | submitting, error, order |
| `OrderStore` | Orders | orders, selectedOrder, loading |

### Store Methods

Stores expose async methods that call services and update state via `patchState`.

```typescript
async loadOrders(buyerId: string): Promise<void> {
  patchState(store, { loading: true, error: null });
  try {
    const orders = await orderService.getOrdersByBuyer(buyerId);
    patchState(store, { orders, loading: false });
  } catch {
    patchState(store, { error: 'Failed to load orders', loading: false });
  }
}
```

### Computed Derivations

Stores expose computed signals for derived state.

```typescript
withComputed((store) => ({
  activeOrders: computed(() =>
    store.orders().filter(o => o.status === 'Submitted' || o.status === 'PaymentProcessing')
  ),
}))
```

## Real-time Communication (SignalR)

The `NotificationService` connects to `/hubs/notifications` via SignalR for real-time order status updates. Currently stubbed — will be activated when Phase 5 (Notification.Worker) is running.

```typescript
@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly orderUpdates = signal<OrderUpdate | null>(null);
  // Will use @microsoft/signalr to connect to the hub
}
```

## Testing

- **Unit Tests**: Vitest with Angular testing utilities
- **Component Tests**: `TestBed` with standalone component imports
- **E2E Tests**: Playwright (Phase 8)

```bash
pnpm test          # Run unit tests
pnpm ng lint       # Lint check
pnpm ng build      # Production build
```

## Design System

See [Design.md](./Design.md) for the complete design system including:
- Color palette (Corporate Trust purple theme)
- Typography (Lexend + Source Sans 3)
- Glassmorphism effects
- Animation specs
- i18n guidelines

## Adding a New Feature

1. Create feature directory: `src/app/features/my-feature/`
2. Create models: `my-feature.models.ts`
3. Create service: `my-feature.service.ts` (HTTP calls to `/api/*`)
4. Create store: `my-feature.store.ts` (NgRx SignalStore)
5. Create components with `standalone: true` and `OnPush`
6. Create routes: `my-feature.routes.ts` (default export)
7. Register routes in `app.routes.ts`
8. Add SSR render mode in `app.routes.server.ts` if dynamic
9. Add Lucide icons to `app.config.ts` if needed
