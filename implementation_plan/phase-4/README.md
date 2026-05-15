# Phase 4 — Sub-Plans Index

Execute these sub-tasks **in order**. Each file contains exact commands, project files, and C# code to copy-paste.

| # | Sub-Plan | Description |
|:--|:---|:---|
| 4.0 | [4.0-ordering-payment-contracts.md](./4.0-ordering-payment-contracts.md) | Payment & Ordering integration contracts (`ProcessPaymentCommand`, `PaymentCompletedEvent`, `PaymentFailedEvent`, `OrderCompletedEvent`, `OrderCancelledEvent`) in SharedContracts |
| 4.1 | [4.1-ordering-domain.md](./4.1-ordering-domain.md) | Ordering.Domain — Order aggregate, OrderItem entity, Address value object, OrderStatus enum |
| 4.2 | [4.2-ordering-application.md](./4.2-ordering-application.md) | Ordering.Application — CreateOrder, CancelOrder, GetOrderById, ListOrdersByBuyer CQRS |
| 4.3 | [4.3-ordering-infrastructure.md](./4.3-ordering-infrastructure.md) | Ordering.Infrastructure — EF Core DbContext, repository, Outbox configuration |
| 4.4 | [4.4-ordering-api.md](./4.4-ordering-api.md) | Ordering.API — `OrderStateMachine` saga, `OrderState` instance, Minimal API endpoints, `Program.cs` with MassTransit v8 |
| 4.5 | [4.5-payment-service.md](./4.5-payment-service.md) | Payment full stack — Domain, Application, Infrastructure, API (`ProcessPaymentConsumer`, mock gateway, payment status endpoint) |
| 4.6 | [4.6-apphost-gateway-wiring.md](./4.6-apphost-gateway-wiring.md) | Wire Ordering + Payment in AppHost, verify YARP routes, saga verification checklist |

## Reference Docs
- Architecture: [`plans/README.md`](../../plans/README.md)
- Domain Decomposition: [`plans/02-domain-decomposition.md`](../../plans/02-domain-decomposition.md)
- Clean Architecture: [`plans/03-clean-architecture.md`](../../plans/03-clean-architecture.md)
- Messaging & Sagas: [`plans/05-messaging-sagas.md`](../../plans/05-messaging-sagas.md)
- API Gateway & BFF: [`plans/06-api-gateway-bff.md`](../../plans/06-api-gateway-bff.md)
- Security: [`plans/08-security.md`](../../plans/08-security.md)
- Phase overview: [`implementation_plan/phase-4-ordering-payment.md`](../phase-4-ordering-payment.md)
