# 📋 Architecture Plans — Enterprise Marketplace

This directory contains the complete architectural documentation for the Enterprise Marketplace microservices platform.

## Document Map

| Document | Description |
|:---|:---|
| [01-system-overview.md](./01-system-overview.md) | High-level system context, vision, and technology stack |
| [02-domain-decomposition.md](./02-domain-decomposition.md) | Microservice topology, bounded contexts, and domain responsibilities |
| [03-clean-architecture.md](./03-clean-architecture.md) | Internal service structure: Clean Architecture, DDD, CQRS |
| [04-monorepo-structure.md](./04-monorepo-structure.md) | Repository layout, directory conventions, BuildingBlocks rules |
| [05-messaging-sagas.md](./05-messaging-sagas.md) | MassTransit, Saga orchestration, Outbox pattern, compensating transactions |
| [06-api-gateway-bff.md](./06-api-gateway-bff.md) | YARP reverse proxy, BFF pattern, cookie-to-bearer transformation |
| [07-realtime-signalr.md](./07-realtime-signalr.md) | SignalR, Redis Backplane, WebSocket routing through YARP |
| [08-security.md](./08-security.md) | Authentication, authorization, Zero Trust, Managed Identities |
| [09-infrastructure-deployment.md](./09-infrastructure-deployment.md) | .NET Aspire, Aspirate, Terraform, Azure Container Apps |
| [10-testing-strategy.md](./10-testing-strategy.md) | Unit, Integration (Testcontainers), E2E (Playwright) |
| [11-frontend-angular.md](./11-frontend-angular.md) | Angular 19+, Signals, NgRx SignalStore, Zard UI |
| [12-c4-diagrams.md](./12-c4-diagrams.md) | C4 architecture diagrams (Context, Container, Component) |

## Architecture Principles

1. **Domain-Driven Design** — Strict bounded context isolation
2. **Database-per-Service** — No shared databases between microservices
3. **Clean Architecture** — Domain independence from infrastructure
4. **CQRS** — Separate read/write models via MediatR
5. **Event-Driven** — Async communication via MassTransit + RabbitMQ/Azure Service Bus
6. **Zero Trust** — Managed Identities for S2S authentication
7. **BFF Security** — HTTP-only cookies, no tokens in browser storage

## Tech Stack Summary

```
┌─────────────────────────────────────────────────────┐
│  Frontend:  Angular 19+ │ Signals │ Zard UI         │
│  Gateway:   YARP (BFF) │ Cookie-to-Bearer           │
│  Backend:   .NET 10 │ C# 14.1 │ Minimal APIs        │
│  Patterns:  Clean Architecture │ DDD │ CQRS(MediatR) │
│  Messaging: MassTransit │ RabbitMQ / Azure SB       │
│  Data:      PostgreSQL │ Redis │ Elasticsearch       │
│  Infra:     .NET Aspire │ Azure Container Apps      │
│  IaC:       Aspirate │ Terraform                    │
│  Testing:   xUnit │ Testcontainers │ Playwright      │
└─────────────────────────────────────────────────────┘
```
