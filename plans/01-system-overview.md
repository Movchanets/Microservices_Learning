# 01 — System Overview

## Vision

Build an enterprise-grade marketplace platform on .NET 10 / C# 14.1 that handles high-volume transactions, fast product search, reliable inventory reservations, and isolated payment processing — all within a distributed microservices ecosystem.

## Key Goals

| Goal | How |
|:---|:---|
| **High Availability** | Horizontally scalable microservices on Azure Container Apps |
| **Scalability** | Independent scaling per bounded context (Database-per-Service) |
| **Security** | BFF pattern (HTTP-only cookies), Zero Trust S2S via Managed Identities |
| **Data Consistency** | MassTransit Sagas with Outbox pattern for guaranteed delivery |
| **Developer Experience** | .NET Aspire orchestration for local dev parity with production |
| **Fast Search** | Elasticsearch synced via integration events from Catalog |

## Technology Stack

### Backend
- **Runtime**: .NET 10, C# 14.1
- **APIs**: ASP.NET Core Minimal APIs
- **ORM**: Entity Framework Core 10
- **CQRS**: MediatR (Commands, Queries, Pipeline Behaviors)
- **Validation**: FluentValidation
- **Messaging**: MassTransit + RabbitMQ (local) / Azure Service Bus (cloud)
- **Saga**: MassTransit Automatonymous State Machines
- **Real-time**: ASP.NET Core SignalR + Redis Backplane

### Frontend
- **Framework**: Angular 19+ (Standalone Components)
- **State**: NgRx SignalStore (Signals-based)
- **UI Components**: Spartan/UI
- **Styling**: Tailwind CSS

### Infrastructure
- **Orchestration**: .NET Aspire (AppHost + ServiceDefaults)
- **Gateway**: YARP (Yet Another Reverse Proxy)
- **Databases**: PostgreSQL (per service), Redis (caching, cart, SignalR backplane)
- **Search**: Elasticsearch
- **Media Storage**: Azure Blob Storage (Azurite emulator locally)
- **Cloud**: Azure Container Apps (ACA)
- **IaC**: Terraform (generated via Aspirate)
- **CI/CD**: GitHub Actions / Azure DevOps

### Testing
- **Unit**: xUnit, Moq, FluentAssertions
- **Integration**: Testcontainers (.NET) with real PostgreSQL, Redis, RabbitMQ
- **E2E**: Playwright (browser automation)

## Architectural Decisions

### ADR-001: MassTransit over Dapr for Messaging
- **Context**: ACA has native Dapr integration, but Dapr lacks robust saga orchestration.
- **Decision**: Use MassTransit for all messaging and sagas. Dapr may be used for state management (Cart) and secret stores.
- **Rationale**: Automatonymous state machines provide superior DSL for complex order workflows with compensating transactions.

### ADR-002: YARP BFF over Token-in-Browser
- **Context**: Storing JWT in localStorage exposes Angular app to XSS attacks.
- **Decision**: Implement BFF on YARP with HTTP-only encrypted session cookies; transform cookies to bearer tokens internally.
- **Rationale**: Eliminates XSS attack vector for token theft while keeping internal services stateless.

### ADR-003: Database-per-Service with Eventual Consistency
- **Context**: Shared databases create coupling and hinder independent deployments.
- **Decision**: Each microservice owns its PostgreSQL database; inter-service data sync via integration events.
- **Rationale**: Enables independent scaling and deployment at the cost of eventual consistency (acceptable for this domain).
