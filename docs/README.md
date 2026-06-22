# Marketplace Microservices — Documentation Index

**Last Updated:** 2026-06-19
**Architecture:** .NET 10 Microservices · Angular 21 SPA · DDD · CQRS · Event-Driven

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
- **Frontend:** Angular 21 + NgRx SignalStore + Spartan/UI + Tailwind CSS

---

## Architecture Docs

| Document | Location | Purpose |
|----------|----------|---------|
| C4 Diagrams | [architecture/c4-diagrams.md](architecture/c4-diagrams.md) | Visual architecture reference |
| Monorepo Layout | [architecture/monorepo-structure.md](architecture/monorepo-structure.md) | Directory rules, BuildingBlocks conventions |
| Messaging & Sagas | [architecture/messaging.md](architecture/messaging.md) | MassTransit patterns, Outbox, compensation |
