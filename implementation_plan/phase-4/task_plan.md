# Phase 4 — Implementation Task Plan

## Goal
Implement the core order lifecycle using MassTransit State Machine saga with compensating transactions, and the payment gateway integration.

## Sub-Plans

| # | File | Status |
|:--|:-----|:-------|
| 4.0 | `4.0-ordering-payment-contracts.md` | `pending` |
| 4.1 | `4.1-ordering-domain.md` | `pending` |
| 4.2 | `4.2-ordering-application.md` | `pending` |
| 4.3 | `4.3-ordering-infrastructure.md` | `pending` |
| 4.4 | `4.4-ordering-api.md` | `pending` |
| 4.5 | `4.5-payment-service.md` | `pending` |
| 4.6 | `4.6-apphost-gateway-wiring.md` | `pending` |

## Decisions
- Follow Phase 1/2/3 sub-plan format exactly (Goal, Ref, Prerequisites, Steps, Verification, Done When)
- Ordering: 4-layer Clean Architecture (Domain, Application, Infrastructure, API)
- Payment: 4-layer Clean Architecture (Domain, Application, Infrastructure, API)
- `OrderStateMachine` saga lives in Ordering.API (presentation layer orchestrates)
- `OrderState` persisted via EF Core in Ordering.Infrastructure DbContext
- All MassTransit configuration uses v8 API style (Catalog is reference impl): `SetKebabCaseEndpointNameFormatter()`, `cfg.Host(builder.Configuration.GetConnectionString("messaging"))`
- Reuse `IRepository<T>`, `IUnitOfWork`, `Result<T>`, `AggregateRoot`, `ValueObject` from SharedContracts/Infrastructure
- `OrderItemContract` already exists in SharedContracts — reuse it
- Payment consumer uses mock gateway (Stripe SDK integration deferred to production)
- `OrderState` saga instance stores items as JSON for simplicity
- YARP routes for `/api/orders/**` and `/api/payments/**` already exist in gateway config
