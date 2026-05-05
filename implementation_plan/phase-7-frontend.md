# Phase 7 — Angular Frontend

**Goal**: Build the Angular SPA with Standalone Components, Signals, NgRx SignalStore, and Spartan/UI.

**Depends on**: Phase 1 (starts incrementally, grows with each backend phase)

## Tasks

- [ ] **Initialize Angular project** in `src/web/` — Angular 19+, standalone, Tailwind CSS
- [ ] **Install dependencies** — Spartan/UI, `@microsoft/signalr`, NgRx SignalStore
- [ ] **Configure Aspire integration** — `AddNpmApp("angular", "../web")` in AppHost
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
- [ ] **Implement admin panel** — User management, seller verification
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
