# Project Status — 2026-05-19

## Summary

The marketplace microservices project is in **strong shape**. All 16 implementation plans are complete. Backend builds clean (0 errors), 254 unit tests + 44 integration tests all pass, frontend has 293 Vitest tests passing. One critical issue remains: 4 contract tests failing due to raw SQL usage with InMemory provider.

## What Happened Recently (May 18-19)

| Commit | Description |
|--------|-------------|
| `2fa1fec` | Replace glassy UI with solid styles |
| `83dfe7a` | Fix doubling messages in seeding |
| `ab4ddb1` | Remove unused test |
| `11ad8fc` | Custom ShoppingCartJsonConverter + GUID PKs + concurrency |
| `7e1df3e` | Cart infrastructure, message consumers, E2E testing suite |
| `805a566` | Phase 5 — Saga-aware cancellation and refunds |
| `92e0e84` | Propagate SellerId through Cart & Ordering |
| `db49c45` | Integration + E2E tests for seller orders |

## Key Metrics

| Metric | Value |
|--------|-------|
| Backend build | ✅ 0 errors, 154 warnings |
| Frontend build | ✅ Success (1 budget warning) |
| Unit tests | ✅ 254/254 |
| Integration tests | ✅ 44/44 |
| Contract tests | ⚠️ 47/51 (4 fail) |
| Frontend tests | ✅ 293/293 |
| E2E specs | 24 files (not run today) |
| Implementation plans | 16/16 complete |
| Git commits (since May 18) | 8 |

## Top 3 Action Items

1. **Fix 4 contract tests** — `ProductPriceRepository.UpsertAsync` uses `ExecuteSqlRawAsync` which fails on InMemory provider. Replace with pure EF Core upsert.
2. **Frontend bundle size** — 589KB exceeds 500KB budget. Needs tree-shaking / lazy loading audit.
3. **Payment outbox** — `PaymentRefundedEvent` published outside MassTransit outbox. Risk of lost messages.
