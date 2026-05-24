# P0 Fixes — Critical Marketplace Gaps

**Purpose**: Fix all P0 (critical) issues from `plans/MISSING.md` to make the basic buyer/seller flow work.

**Created**: 2026-05-15

## Plans

| # | File | Fixes | Summary |
|---|------|-------|---------|
| 01 | `01-auth-and-guards.md` | #1.1, #1.2, #1.4, #2.6, #4.3 | JWT auth on all endpoints + frontend AuthGuard/RoleGuard |
| 02 | `02-identity-events.md` | #1.5, #4.1, #4.2 | Identity MassTransit + StoreVerified → role update pipeline |
| 03 | `03-order-flow-fixes.md` | #2.1, #2.2, #2.5 | Fix price resolution, add address, add order cancellation |
| 04 | `04-cart-hardening.md` | #2.3, #2.4 | JWT auth on Cart + MassTransit Outbox |
| 05 | `05-signalr-connection.md` | #3.1, #3.2, #3.3 | Start SignalR in frontend + fix broadcast targeting |
| 06 | `06-health-endpoints.md` | #7.1 | Uncomment all services in gateway health check |

## Dependencies Between Plans

```
01 (Auth) ──→ 04 (Cart Auth) — Cart auth depends on JWT being set up
02 (Identity Events) ──→ 01 (Auth) — Role updates need auth policies
03 (Order Flow) ──→ 04 (Cart) — Price resolution needs Cart changes
05 (SignalR) ──→ 01 (Auth) — SignalR needs authenticated user ID
06 (Health) — independent, can be done anytime
```

## Suggested Order

1. **06 — Health Endpoints** (5 min, independent, quick win)
2. **01 — Auth & Guards** (biggest impact, enables everything else)
3. **04 — Cart Hardening** (depends on 01)
4. **03 — Order Flow Fixes** (depends on 04 for prices)
5. **02 — Identity Events** (enables seller verification flow)
6. **05 — SignalR Connection** (polish, can be done in parallel with 02-04)
