# 11 — Frontend: Angular Architecture

## Stack

| Layer | Technology |
|:---|:---|
| Framework | Angular 19+ (Standalone Components) |
| State Management | NgRx SignalStore (Signals-based) |
| UI Components | Zard UI |
| Styling | Tailwind CSS |
| Real-time | SignalR client (`@microsoft/signalr`) |
| Build/Dev | .NET Aspire AppHost (`AddNpmApp`) |

## Key Principles

1. **No NgModules** — All components are `standalone: true`
2. **Signals** — Primary reactivity primitive (no RxJS for local state)
3. **Lazy Loading** — Route-level code splitting
4. **BFF Integration** — All API calls go through YARP with `withCredentials: true`

## Project Structure

```
src/web/
├── src/
│   ├── app/
│   │   ├── core/                    # Singleton services, guards, interceptors
│   │   │   ├── auth/
│   │   │   │   ├── auth.service.ts
│   │   │   │   └── auth.guard.ts
│   │   │   ├── http/
│   │   │   │   └── api.interceptor.ts
│   │   │   └── signalr/
│   │   │       └── notification.service.ts
│   │   │
│   │   ├── features/                # Feature modules (lazy loaded)
│   │   │   ├── catalog/
│   │   │   │   ├── catalog.routes.ts
│   │   │   │   ├── catalog.store.ts     # NgRx SignalStore
│   │   │   │   ├── catalog-list/
│   │   │   │   └── product-detail/
│   │   │   ├── cart/
│   │   │   ├── checkout/
│   │   │   ├── orders/
│   │   │   ├── seller-dashboard/
│   │   │   └── admin/
│   │   │
│   │   ├── shared/                  # Reusable components, pipes, directives
│   │   │   ├── components/
│   │   │   ├── pipes/
│   │   │   └── directives/
│   │   │
│   │   ├── app.component.ts
│   │   ├── app.config.ts
│   │   └── app.routes.ts
│   │
│   ├── styles/                      # Tailwind + global styles
│   └── environments/
│
├── angular.json
├── tailwind.config.js
├── tsconfig.json
└── package.json
```

## NgRx SignalStore Example

```typescript
// features/catalog/catalog.store.ts
export const CatalogStore = signalStore(
  withState<CatalogState>({
    products: [],
    loading: false,
    searchQuery: '',
    selectedCategory: null,
  }),
  withComputed((store) => ({
    filteredProducts: computed(() =>
      store.products().filter(p =>
        p.name.toLowerCase().includes(store.searchQuery().toLowerCase())
      )
    ),
  })),
  withMethods((store, catalogService = inject(CatalogService)) => ({
    async loadProducts(): Promise<void> {
      patchState(store, { loading: true });
      const products = await catalogService.getAll();
      patchState(store, { products, loading: false });
    },
    setSearch(query: string): void {
      patchState(store, { searchQuery: query });
    },
  }))
);
```

## SignalR Integration

```typescript
// core/signalr/notification.service.ts
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private hubConnection: HubConnection;

  readonly orderUpdates = signal<OrderUpdate | null>(null);

  constructor() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', { withCredentials: true })
      .withAutomaticReconnect()
      .build();
  }

  async start(): Promise<void> {
    this.hubConnection.on('OrderUpdate', (update: OrderUpdate) => {
      this.orderUpdates.set(update);
    });
    await this.hubConnection.start();
  }
}
```

## API Communication

All HTTP calls use `withCredentials: true` — the BFF session cookie is sent automatically:

```typescript
// core/http/api.interceptor.ts
export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const apiReq = req.clone({ withCredentials: true });
  return next(apiReq);
};
```
