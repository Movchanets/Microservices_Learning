# Phase 2 — Sub-Plans Index

Execute these sub-tasks **in order**. Each file contains exact commands, project files, and C# code to copy-paste.

| # | Sub-Plan | Description |
|:--|:---|:---|
| 2.0 | [2.0-catalog-contracts.md](./2.0-catalog-contracts.md) | Integration event contracts (`ProductCreatedEvent`, `ProductUpdatedEvent`, `ProductDeletedEvent`) in SharedContracts |
| 2.1 | [2.1-catalog-domain.md](./2.1-catalog-domain.md) | Catalog.Domain — Product aggregate, Category entity, Money/Sku value objects |
| 2.2 | [2.2-catalog-application.md](./2.2-catalog-application.md) | Catalog.Application — CreateProduct, UpdateProduct, DeleteProduct, GetProduct, ListProducts CQRS |
| 2.3 | [2.3-catalog-infrastructure.md](./2.3-catalog-infrastructure.md) | Catalog.Infrastructure — EF Core, MassTransit Outbox, repository, event publishing |
| 2.4 | [2.4-catalog-api.md](./2.4-catalog-api.md) | Catalog.API — Minimal API endpoints, Program.cs wiring |
| 2.5 | [2.5-search-api.md](./2.5-search-api.md) | Search.API — Thin service with Elasticsearch, MassTransit consumers |
| 2.6 | [2.6-apphost-gateway-wiring.md](./2.6-apphost-gateway-wiring.md) | Wire Catalog + Search in AppHost, verify gateway routing, integration tests |

## Reference Docs
- Architecture: [`plans/README.md`](../../plans/README.md)
- Domain Decomposition: [`plans/02-domain-decomposition.md`](../../plans/02-domain-decomposition.md)
- Clean Architecture: [`plans/03-clean-architecture.md`](../../plans/03-clean-architecture.md)
- Messaging & Sagas: [`plans/05-messaging-sagas.md`](../../plans/05-messaging-sagas.md)
- API Gateway & BFF: [`plans/06-api-gateway-bff.md`](../../plans/06-api-gateway-bff.md)
- Phase overview: [`implementation_plan/phase-2-catalog-search.md`](../phase-2-catalog-search.md)
