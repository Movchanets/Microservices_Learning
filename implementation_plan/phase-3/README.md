# Phase 3 — Sub-Plans Index

Execute these sub-tasks **in order**. Each file contains exact commands, project files, and C# code to copy-paste.

| # | Sub-Plan | Description |
|:--|:---|:---|
| 3.0 | [3.0-inventory-contracts.md](./3.0-inventory-contracts.md) | Integration event/command contracts (`ReserveInventoryCommand`, `CancelReservationCommand`, `InventoryReservedEvent`, etc.) in SharedContracts |
| 3.1 | [3.1-inventory-domain.md](./3.1-inventory-domain.md) | Inventory.Domain — InventoryItem aggregate, Reservation entity, value objects |
| 3.2 | [3.2-inventory-application.md](./3.2-inventory-application.md) | Inventory.Application — ReserveStock, ReleaseStock, GetStockBySku CQRS |
| 3.3 | [3.3-inventory-infrastructure.md](./3.3-inventory-infrastructure.md) | Inventory.Infrastructure — EF Core, optimistic locking, MassTransit Outbox, consumers |
| 3.4 | [3.4-inventory-api.md](./3.4-inventory-api.md) | Inventory.API — Minimal API endpoints, Program.cs wiring |
| 3.5 | [3.5-cart-api.md](./3.5-cart-api.md) | Cart.API — Thin service with Redis, cart CRUD, checkout flow |
| 3.6 | [3.6-apphost-gateway-wiring.md](./3.6-apphost-gateway-wiring.md) | Wire Inventory + Cart in AppHost, verify gateway routing, integration tests |

## Reference Docs
- Architecture: [`plans/README.md`](../../plans/README.md)
- Domain Decomposition: [`plans/02-domain-decomposition.md`](../../plans/02-domain-decomposition.md)
- Clean Architecture: [`plans/03-clean-architecture.md`](../../plans/03-clean-architecture.md)
- Messaging & Sagas: [`plans/05-messaging-sagas.md`](../../plans/05-messaging-sagas.md)
- API Gateway & BFF: [`plans/06-api-gateway-bff.md`](../../plans/06-api-gateway-bff.md)
- Phase overview: [`implementation_plan/phase-3-inventory-cart.md`](../phase-3-inventory-cart.md)
