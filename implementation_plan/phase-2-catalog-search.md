# Phase 2 — Catalog.API & Search.API

**Goal**: Build the product catalog (source of truth) and the Elasticsearch-backed search service, connected via integration events.

**Depends on**: Phase 1

## Catalog.API Tasks

- [ ] **Scaffold Clean Architecture projects**
  - `Catalog.Domain/` — Product aggregate, Category entity, value objects (Money, Sku)
  - `Catalog.Application/` — CreateProduct, UpdateProduct, DeleteProduct, GetProductById, ListProducts
  - `Catalog.Infrastructure/` — EF Core DbContext with `catalog-db`, migrations, repository
  - `Catalog.API/` — Minimal API endpoints with OpenAPI docs
- [ ] **Define integration event contracts** in `SharedContracts`
  - `ProductCreatedEvent(Guid ProductId, string Name, decimal Price, string Category, ...)`
  - `ProductUpdatedEvent(Guid ProductId, string Name, decimal Price, string Category, ...)`
  - `ProductDeletedEvent(Guid ProductId)`
- [ ] **Configure MassTransit Outbox** — Publish events in same transaction as DB save
- [ ] **Add YARP route** `/api/catalog/**` → Catalog.API
- [ ] **Register in AppHost** with `catalog-db` and `messaging`
- [ ] **Write unit tests** — Product aggregate invariants (price > 0, SKU format)
- [ ] **Write integration tests** — Create product → verify DB + event published

## Search.API Tasks

- [ ] **Create Search.API project** in `src/Microservices/Search/Search.API/`
  - No Clean Architecture layers needed — thin service
  - Elasticsearch client configuration
  - Search endpoints: `GET /api/search?q=...&category=...&priceMin=...&priceMax=...`
- [ ] **Implement MassTransit consumers**
  - `ProductCreatedConsumer` → Index new document in Elasticsearch
  - `ProductUpdatedConsumer` → Update existing document
  - `ProductDeletedConsumer` → Remove document from index
- [ ] **Implement search features**
  - Full-text search with relevance scoring
  - Faceted filtering (category, price range, attributes)
  - Pagination support
- [ ] **Add YARP route** `/api/search/**` → Search.API
- [ ] **Register in AppHost** with Elasticsearch and `messaging`
- [ ] **Write integration tests** — Publish event → verify Elasticsearch document indexed

## Deliverables
```
src/Microservices/
├── Catalog/
│   ├── Catalog.Domain/
│   ├── Catalog.Application/
│   ├── Catalog.Infrastructure/
│   └── Catalog.API/
└── Search/
    └── Search.API/
```
