# Marketplace Microservices — Documentation Index

**Last Updated:** 2026-05-31
**Architecture:** .NET 10 Microservices · Angular 19 SPA · DDD · CQRS · Event-Driven

---

## Quick Navigation

| Section | Description |
|---------|-------------|
| [Backend Services](backend/) | Per-service architecture, endpoints, domain models |
| [Frontend](frontend/) | Angular SPA features, state management, components |
| [Testing](testing/) | Test coverage, plans, gaps per service |
| [Testing/Playwright](testing/playwright/) | Playwright POM patterns, fixtures, component objects |
| [Architecture](architecture/) | C4 diagrams, system design, patterns |

---

## Service Map

| Service | Type | Database | Key Tech |
|---------|------|----------|----------|
| Identity | Full (4-layer) | PostgreSQL | JWT, BCrypt, MediatR |
| Catalog | Full (4-layer) | PostgreSQL | EF Core, Domain Events |
| Search | Thin | Elasticsearch | Nest client |
| Inventory | Full (4-layer) | PostgreSQL | Optimistic locking |
| Cart | Thin | Redis | StackExchange.Redis |
| Ordering | Full (4-layer) | PostgreSQL | MassTransit Saga |
| Payment | Full (4-layer) | PostgreSQL | Stripe integration |
| StoreManagement | Full (4-layer) | PostgreSQL | Seller onboarding |
| Media | Thin | Azure Blob | Binary storage, Gallery CRUD |
| Notification | Worker | — | SignalR + Redis Backplane |

---

## Infrastructure

- **Orchestration:** .NET Aspire (AppHost)
- **Gateway:** YARP (BFF pattern) with Cookie-to-Bearer auth
- **Messaging:** MassTransit (RabbitMQ / Azure Service Bus)
- **Frontend:** Angular 19 + NgRx SignalStore + Spartan/UI + Tailwind CSS

---

## Plans & Architecture Docs

| Document | Location | Purpose |
|----------|----------|---------|
| Media API Architecture | `plans/media-api/README.md` | Media service design, data flow, endpoints |
| Rozetka Scraper | `plans/rozetka-scraper/` | Scraper architecture, quickstart, technical details |
| C4 Diagrams | `plans/12-c4-diagrams.md` | Visual architecture reference |
| Monorepo Layout | `plans/04-monorepo-structure.md` | Directory rules, BuildingBlocks conventions |
| Clean Architecture | `plans/03-clean-architecture.md` | Layer dependencies, code templates |
| Messaging & Sagas | `plans/05-messaging-sagas.md` | MassTransit patterns, Outbox, compensation |
| API Gateway & BFF | `plans/06-api-gateway-bff.md` | YARP routes, cookie-to-bearer flow |
| Security | `plans/08-security.md` | Zero Trust, CSRF, Managed Identities |
| Frontend Angular | `plans/11-frontend-angular.md` | Signals, SignalStore, project structure |
