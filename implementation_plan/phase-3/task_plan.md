# Phase 3 — Implementation Task Plan

## Goal
Build Inventory.API (stock reservation with optimistic locking) and Cart.API (Redis-backed shopping cart) following Phase 1/2 patterns.

## Sub-Plans

| # | File | Status |
|:--|:-----|:-------|
| 3.0 | `3.0-inventory-contracts.md` | `pending` |
| 3.1 | `3.1-inventory-domain.md` | `pending` |
| 3.2 | `3.2-inventory-application.md` | `pending` |
| 3.3 | `3.3-inventory-infrastructure.md` | `pending` |
| 3.4 | `3.4-inventory-api.md` | `pending` |
| 3.5 | `3.5-cart-api.md` | `pending` |
| 3.6 | `3.6-apphost-gateway-wiring.md` | `pending` |

## Decisions
- Follow Phase 1/2 sub-plan format exactly (Goal, Ref, Prerequisites, Steps, Verification, Done When)
- Inventory: 4-layer Clean Architecture (Domain, Application, Infrastructure, API)
- Cart: Thin service (single API project, direct Redis operations)
- Reuse `IRepository<T>`, `IUnitOfWork`, `Result<T>`, `AggregateRoot`, `ValueObject` from SharedContracts/Infrastructure
- MassTransit Outbox on Inventory.Infrastructure (same pattern as Catalog.Infrastructure)
- `OrderItemContract` shared contract in `3.0-inventory-contracts.md`
