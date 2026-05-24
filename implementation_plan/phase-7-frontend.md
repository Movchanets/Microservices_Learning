# Phase 7 — Angular Frontend

**Goal**: Build the Angular SPA with Standalone Components, Signals, NgRx SignalStore, and Spartan/UI.

**Depends on**: Phase 1 (starts incrementally, grows with each backend phase)
**Design Reference**: Please refer to `src/web/Design.md` for styling, theming (light/dark), and i18n guidelines.

## Tasks

- [ ] **Initialize Angular project** in `src/web/` — Angular 21, standalone, Tailwind CSS v4, `pnpm`
- [ ] **Install dependencies** — `@spartan-ng`, `@microsoft/signalr`, NgRx SignalStore, `@angular/localize`
- [ ] **Configure Aspire integration** — `AddNpmApp("angular", "../web", "pnpm run start")` in AppHost
- [ ] **Implement core layer** (`src/app/core/`)
  - Auth service + guard (BFF cookie flow, `withCredentials: true`)
  - API interceptor (credentials, error handling)
  - SignalR notification service (connect to `/hubs/notifications`)
- [ ] **Implement auth features** — Login, register, profile pages
- [ ] **Implement catalog features** — Product list (with SignalStore), product detail, search + filters
- [ ] **Implement cart feature** — Add/remove items, quantity update, checkout button
- [ ] **Implement checkout flow** — Order summary → confirm → real-time status via SignalR
- [ ] **Implement order history** — List past orders with status badges
- [ ] **Implement seller dashboard** — Product management, store settings, sales overview
- [ ] **Implement admin panel** — User management, seller verification → see `phase-7/7.6/` (3 sub-plans: models/services, store, UI)
- [ ] **Configure lazy loading** — Route-level code splitting per feature
- [ ] **Verify** — Full user journey: register → browse → cart → checkout → notification

## Deliverables
```
src/web/
├── src/app/
│   ├── core/        (auth, http, signalr)
│   ├── features/    (catalog, cart, checkout, orders, seller, admin)
│   └── shared/      (reusable components)
├── angular.json
└── tailwind.config.js
```
