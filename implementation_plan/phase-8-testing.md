# Phase 8 — Testing & Hardening

**Goal**: Comprehensive test coverage, performance validation, and security hardening.

**Depends on**: Phases 5, 6, 7

## Tasks

- [ ] **Achieve 80%+ unit test coverage** on all Domain layers
  - Catalog.Domain, Ordering.Domain, Inventory.Domain, Payment.Domain, StoreManagement.Domain
  - Verify aggregate invariants, value object equality, domain event generation
- [ ] **Complete integration test suite** with Testcontainers
  - All repositories: CRUD against real PostgreSQL
  - All MassTransit consumers: message delivery with real RabbitMQ
  - Saga: full happy path + compensation with real DB + broker
  - Cart: Redis operations
- [ ] **Write E2E tests** with Playwright
  - BFF cookie authentication flow (login → cookie → API access)
  - Buyer journey: browse → search → cart → checkout → order confirmation
  - Seller journey: login → create product → verify listing in catalog
  - Real-time: verify SignalR notification appears after order completion
- [ ] **Security hardening**
  - Verify CSRF protection on all mutating endpoints
  - Verify CORS only allows configured origins
  - Verify rate limiting on public endpoints
  - Verify no tokens exposed in browser DevTools
- [ ] **Performance baseline**
  - Load test key endpoints (search, catalog listing, checkout)
  - Verify Elasticsearch query latency < 100ms for standard searches
  - Verify Redis cart operations < 10ms
- [ ] **Fix all failing tests and regressions**

## Test Directory
```
tests/
├── UnitTests/
│   ├── Catalog.Domain.Tests/
│   ├── Ordering.Domain.Tests/
│   ├── Inventory.Domain.Tests/
│   └── Payment.Domain.Tests/
├── IntegrationTests/
│   ├── Catalog.IntegrationTests/
│   ├── Ordering.IntegrationTests/
│   ├── Inventory.IntegrationTests/
│   └── Identity.IntegrationTests/
└── E2ETests/
    ├── playwright.config.ts
    ├── tests/
    └── pages/
```
