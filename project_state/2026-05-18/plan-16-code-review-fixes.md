# Plan 16: Code Review Fixes — Critical Issues from Session Review

## Goal
Fix all 3 CRITICAL issues and 1 WARNING identified during comprehensive code review of Plans 11-15 changes.

## Context
- **Source:** Full code review of 56 changed files across Plans 11-15
- **Review date:** 2026-05-18
- **Review grade:** B+ (good DDD patterns, concurrency and error handling needed hardening)

## Issues Found & Fixed

### C1. TOCTOU Race Condition in Refund Handler — FIXED
**Severity:** CRITICAL
**File:** `src/Microservices/Payment/Payment.Application/Commands/RefundPayment/RefundPaymentHandler.cs`

**Problem:** Handler reads existing refunds, checks total, then writes — no DB-level protection. Two concurrent refund requests could both pass the totalRefunded check before either writes, allowing over-refunding.

**Fix:** Added retry loop in `PaymentEndpoints.cs` (API layer) that catches `DbUpdateConcurrencyException` and retries up to 2 times. Handler stays clean (no EF Core dependency in Application layer — Clean Architecture).

```csharp
// PaymentEndpoints.cs — retry on concurrency conflict
for (var attempt = 0; attempt <= MaxRefundRetries; attempt++)
{
    try
    {
        result = await sender.Send(cmd, ct);
        break;
    }
    catch (DbUpdateConcurrencyException) when (attempt < MaxRefundRetries)
    {
        // Concurrent refund modified data — retry with fresh read
    }
}
```

**Files modified:**
- `src/Microservices/Payment/Payment.API/Endpoints/PaymentEndpoints.cs`

---

### C2. Buyer and Admin Are Same User in E2E Tests — FIXED
**Severity:** CRITICAL
**Files:** `tests/E2ETests/data/users.json`, `src/Microservices/Identity/Identity.Infrastructure/Persistence/DatabaseMigrationExtensions.cs`

**Problem:** `users.buyerUser.email == "admin@marketplace.com"` — same as `adminUser`. Auth fixture logged in as same user for both roles. The "non-admin cannot issue refund" test was actually testing an admin calling the endpoint, masking authorization bugs.

**Fix:**
1. Added `buyer@marketplace.com` (role=Buyer) to Identity seed data
2. Updated `users.json` with distinct buyer credentials
3. `validUser` now points to buyer (not admin)

```csharp
// DatabaseMigrationExtensions.cs — new buyer seed
var buyerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
var buyer = User.Create("buyer@marketplace.com", hasher.Hash("P@ssw0rd123!"),
    "Test", "Buyer", UserRole.Buyer, userId: buyerId);
```

**Files modified:**
- `src/Microservices/Identity/Identity.Infrastructure/Persistence/DatabaseMigrationExtensions.cs`
- `tests/E2ETests/data/users.json`

---

### C3. Saga Swallows Refund Failures Silently — FIXED
**Severity:** CRITICAL
**File:** `src/Microservices/Payment/Payment.Infrastructure/Messaging/RefundPaymentConsumer.cs`

**Problem:** When refund failed, consumer logged a warning and returned normally. MassTransit considered the message consumed successfully — no retry. System stuck in inconsistent state (order cancelled, payment not refunded).

**Fix:** Consumer now throws `InvalidOperationException` on failure, triggering MassTransit redelivery policy.

```csharp
// Before (swallowed failure):
logger.LogWarning("Refund failed for Order {OrderId}: {Error}", cmd.OrderId, result.Error);

// After (throws for retry):
logger.LogError("Refund failed for Order {OrderId}: {Error} — throwing for MassTransit retry", cmd.OrderId, result.Error);
throw new InvalidOperationException($"Refund failed for Order {cmd.OrderId}: {result.Error}");
```

**Files modified:**
- `src/Microservices/Payment/Payment.Infrastructure/Messaging/RefundPaymentConsumer.cs`

---

### W1. Missing State Guards on Refund Entity — FIXED
**Severity:** WARNING
**File:** `src/Microservices/Payment/Payment.Domain/Aggregates/Refund.cs`

**Problem:** `MarkProcessed()` and `MarkFailed()` had no state validation. A refund could be processed twice, or failed after already being processed.

**Fix:** Added state guards requiring `Status == Pending` before transition.

```csharp
public void MarkProcessed(string gatewayRefundId)
{
    if (Status != RefundStatus.Pending)
        throw new InvalidOperationException($"Cannot process refund in {Status} state");
    // ...
}
```

**Files modified:**
- `src/Microservices/Payment/Payment.Domain/Aggregates/Refund.cs`

---

## Remaining Warnings (Not Fixed — Lower Priority)

| ID | Issue | Severity | Status |
|----|-------|----------|--------|
| W2 | Saga sends TransactionId=Guid.Empty in refund command | Low | Deferred — consumer looks up by OrderId |
| W3 | PaymentRefundedEvent published outside outbox | Medium | Deferred — needs MassTransit outbox refactor |
| W4 | ProductUpdatedConsumer hardcodes "USD" currency | Low | Deferred |
| W5 | CartRepository retry loop falls through on exhaustion | Low | Deferred |
| W6 | GET /api/payments/order/{orderId} has no ownership check | Medium | Deferred — needs auth policy |
| W7 | BuyerIdUserIdProvider query string fallback | Low | Deferred — has removal deadline |
| W8 | Jwt:Secret! null-forgiving operator | Low | Deferred |
| W9 | 3 waitForTimeout remnants in page objects | Low | Deferred — 100-150ms each |
| W10 | payment-refund.spec.ts sequential dependencies | Low | Deferred |

## Remaining Suggestions (Not Implemented)

| ID | Suggestion | Status |
|----|-----------|--------|
| S1 | Remove dead code in handler (redundant check) | Done — handler reverted to clean form |
| S2 | Add composite index (TransactionId, Status) on Refunds | Deferred |
| S3 | Align ES memory settings across test/AppHost | Deferred |
| S4 | api-helpers.ts hardcodes baseUrl | Deferred |
| S5 | Extract createAuthenticatedContext helper | Deferred |
| S6 | Store TransactionId in saga state | Deferred |
| S7 | Add IdempotencyKey to Refund | Deferred |
| S8 | Add negative tests for RefundPaymentConsumer | Deferred |
| S9 | Publish RefundFailedEvent on consumer failure | Deferred |
| S10 | Remove dead db-helpers.ts | Deferred |

## Test Results
- Payment unit tests: 30 passed, 0 failed
- Contract tests: 51 passed, 0 failed
- Full solution build: 0 errors
