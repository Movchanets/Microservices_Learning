# Findings & Decisions — 2026-05-17

## Requirements
- Close all residual gaps from ordering flow audit
- Each fix must be verified by an E2E test that simulates the real scenario
- Plans follow the `plans/next_steps/` format (Goal, Context, Backend Changes, Frontend Changes, E2E Verification)

## Research Findings

### Ordering Flow Audit (2026-05-17)
- 5 fixes already applied (address forwarding, SignalR targeting/lifecycle, order projection sync, payment failure persistence)
- 2 residual gaps remain (seller correlation, saga-aware cancellation)
- 1 P1 (refund endpoint)
- 6 Search.IntegrationTests failing (no Elasticsearch)

### E2E Test Infrastructure
- Playwright with Page Object Model pattern
- Fixtures: `test-base.ts` (page objects), `checkout.fixture.ts` (store/product/cart API fixtures)
- `api-helpers.ts` has `registerApi`, `loginApi`, `createStore`, `verifyStore`, `createProduct`, `addToCart`
- Tests run against real backend (Aspire AppHost)
- Pre-existing issue: Playwright `fill()` doesn't trigger Angular reactive form change detection

### SellerId Flow Analysis
- `Catalog.Domain.Product` has `StoreId` (not SellerId directly)
- `CartItem` has Sku, Quantity, Price — no SellerId
- `OrderItemContract` has Sku, Quantity, Price — no SellerId
- `Order.AddItem(sku, name, price, qty)` — no SellerId parameter
- **Gap:** SellerId never propagates from product → cart → order

### Saga Cancellation Analysis
- `CancelOrderHandler` updates order aggregate directly via `order.Cancel(reason)`
- Does NOT publish any integration event
- Saga (`OrderStateMachine`) has no `CancelOrder` event
- Inventory reservation is not released on cancel
- Payment is not rolled back on cancel

### Payment Domain Analysis
- `PaymentTransaction` has `MarkCompleted()` and `MarkFailed()` methods
- No `Refund` entity or `RefundTransaction` concept
- `ProcessPaymentConsumer` handles `ProcessPaymentCommand` → publishes `PaymentCompletedEvent` or `PaymentFailedEvent`
- No refund consumer or endpoint

### SignalR Auth Analysis
- `NotificationHub` has no `[Authorize]` attribute
- `Notification.Worker/Program.cs` has no `UseAuthentication()`/`UseAuthorization()`
- `BuyerIdUserIdProvider` reads from query string — anyone can connect with any buyerId
- No JWT validation on WebSocket handshake

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| Add `SellerId` to `CartItem` and `OrderItemContract` | Clean propagation through the event chain |
| Add `CancelOrderEvent` to saga | Enables saga compensation (inventory release, payment rollback) |
| Create `Refund` aggregate in Payment domain | DDD-compliant refund tracking |
| Use Elasticsearch Testcontainer | Matches existing Testcontainer pattern (PostgreSQL, RabbitMQ, Redis) |
| Add JWT Bearer auth to Notification.Worker | Consistent with other services |

## Resources
- Audit file: `project_state/2026-05-17/ordering-flow-audit.md`
- Existing plans: `plans/next_steps/01-09`
- E2E fixtures: `tests/E2ETests/fixtures/`
- API helpers: `tests/E2ETests/utils/api-helpers.ts`
