# Marketplace Microservices — Documentation Index

**Last Updated:** 2026-05-26  
**Architecture:** .NET 10 Microservices · Angular 19 SPA · DDD · CQRS · Event-Driven

---

## Quick Navigation

| Section | Description |
|---------|-------------|
| [Backend Services](backend/) | Per-service architecture, endpoints, domain models |
| [Frontend](frontend/) | Angular SPA features, state management, components |
| [Testing](testing/) | Test coverage, plans, gaps per service |
| [Architecture](architecture/) | C4 diagrams, system design, patterns |
| [Status](status/) | Current blockers, known issues, project state |

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
| Media | Thin | Azure Blob | Binary storage |
| Notification | Worker | — | SignalR + Redis Backplane |

---

## Infrastructure

- **Orchestration:** .NET Aspire (AppHost)
- **Gateway:** YARP (BFF pattern) with Cookie-to-Bearer auth
- **Messaging:** MassTransit (RabbitMQ / Azure Service Bus)
- **Frontend:** Angular 19 + NgRx SignalStore + Spartan/UI + Tailwind CSS
