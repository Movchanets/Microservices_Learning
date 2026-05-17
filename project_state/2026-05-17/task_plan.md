# Task Plan: Ordering Flow Gaps — E2E-Verified Plans

## Goal
Close all residual gaps from the 2026-05-17 ordering flow audit, with each fix verified by a Playwright E2E test that simulates the real user scenario.

## Current Phase
Phase 1 — Plans created, ready for implementation

## Phases

### Phase 1: Plan Creation
- [x] Review ordering-flow-audit.md findings
- [x] Run all tests (299 backend, 293 frontend)
- [x] Identify 5 residual gaps
- [x] Create plan files with E2E verification specs
- **Status:** complete

### Phase 2: Plan 10 — Seller Order Correlation
- [ ] Propagate SellerId through checkout → OrderItem
- [ ] E2E: seller sees order after buyer checkout
- **Status:** pending

### Phase 3: Plan 11 — Saga-Aware Cancellation
- [ ] CancelOrderHandler coordinates with saga compensation
- [ ] E2E: buyer cancels, inventory released, payment rolled back
- **Status:** pending

### Phase 4: Plan 12 — Payment Refund Endpoint
- [ ] POST /api/payments/{id}/refund endpoint
- [ ] E2E: admin triggers refund, buyer sees refund record
- **Status:** pending

### Phase 5: Plan 13 — Search Integration Test Fix
- [ ] Add Elasticsearch Testcontainer
- [ ] E2E: search returns results after product creation
- **Status:** pending

### Phase 6: Plan 14 — SignalR Hub Authentication
- [ ] Add auth middleware to Notification.Worker
- [ ] E2E: unauthenticated WebSocket rejected, authenticated receives updates
- **Status:** pending

## Key Questions
1. How does SellerId flow from Catalog → Cart → Order? (Answer: via CartItem and OrderItemContract)
2. Does CancelOrderCommand already publish to saga? (Answer: No — it updates aggregate directly)
3. What's the refund flow? (Answer: Payment domain has no Refund entity yet)
4. Why do Search.IntegrationTests fail? (Answer: No Elasticsearch instance in test environment)
5. How does SignalR auth work without middleware? (Answer: BuyerIdUserIdProvider from query string — no real auth)

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Each plan gets its own E2E spec file | Isolates scenarios, makes regression visible |
| Use API helpers for setup (register, login, create store/product) | Existing pattern in checkout.fixture.ts |
| E2E tests use real backend (Aspire AppHost) | Catches integration issues that unit tests miss |
| Plans numbered 10-14 (continuing from 01-09) | Maintains sequence with existing plan index |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| Search.IntegrationTests fail (6/6) | 1 | Need Elasticsearch Testcontainer — Plan 13 |
