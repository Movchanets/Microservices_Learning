# Plan 12: Payment Refund Endpoint

## Goal
Add a refund endpoint to Payment.API so completed payments can be reversed, and the refund is tracked as a domain entity.

## Context
- **Current state:** `PaymentTransaction` has `MarkCompleted()` and `MarkFailed()` but no refund concept. `GET /api/payments/order/{id}` returns transactions but no refund records. No refund endpoint exists.
- **Target state:** `POST /api/payments/{transactionId}/refund` creates a `RefundTransaction`, marks original as refunded. `GET /api/payments/order/{id}` includes refund records.
- **Root cause:** Payment domain was built for forward-only flow. Refund was deferred as P1.

## Prerequisites
- `PaymentTransaction` aggregate exists — `Payment.Domain/Aggregates/`
- `IPaymentTransactionRepository` exists — `Payment.Domain/`
- `PaymentDbContext` exists — `Payment.Infrastructure/Persistence/`
- `GET /api/payments/order/{orderId}` endpoint exists — `Payment.API/Endpoints/`

## Backend Changes

### 1. Create Refund Aggregate
**File:** `src/Microservices/Payment/Payment.Domain/Aggregates/Refund.cs`

```csharp
public sealed class Refund : Entity<Guid>
{
    public Guid TransactionId { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public RefundStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? GatewayRefundId { get; private set; }

    public static Refund Create(Guid transactionId, Guid orderId, decimal amount, string reason)
    {
        return new Refund
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            OrderId = orderId,
            Amount = amount,
            Reason = reason,
            Status = RefundStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed(string gatewayRefundId)
    {
        Status = RefundStatus.Processed;
        GatewayRefundId = gatewayRefundId;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = RefundStatus.Failed;
        Reason = reason;
        ProcessedAt = DateTime.UtcNow;
    }
}

public enum RefundStatus { Pending, Processed, Failed }
```

### 2. Create Refund Repository Interface
**File:** `src/Microservices/Payment/Payment.Domain/Abstractions/IRefundRepository.cs`

```csharp
public interface IRefundRepository
{
    Task<Refund?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Refund>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    void Add(Refund refund);
}
```

### 3. Implement Refund Repository
**File:** `src/Microservices/Payment/Payment.Infrastructure/Persistence/RefundRepository.cs`

EF Core implementation using `PaymentDbContext`.

### 4. Update PaymentDbContext
**File:** `src/Microservices/Payment/Payment.Infrastructure/Persistence/PaymentDbContext.cs`

Add `DbSet<Refund> Refunds`.

### 5. Add EF Configuration for Refund
**File:** `src/Microservices/Payment/Payment.Infrastructure/Persistence/Configurations/RefundConfiguration.cs`

### 6. Create Refund Command + Handler
**File:** `src/Microservices/Payment/Payment.Application/Commands/RefundPayment/RefundPaymentCommand.cs`

```csharp
public record RefundPaymentCommand(Guid TransactionId, string Reason) : IRequest<Result<Guid>>;
```

**File:** `src/Microservices/Payment/Payment.Application/Commands/RefundPayment/RefundPaymentHandler.cs`

```csharp
public sealed class RefundPaymentHandler(
    IPaymentTransactionRepository transactionRepo,
    IRefundRepository refundRepo,
    IUnitOfWork uow) : IRequestHandler<RefundPaymentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RefundPaymentCommand request, CancellationToken ct)
    {
        var transaction = await transactionRepo.GetByIdAsync(request.TransactionId, ct);
        if (transaction is null) return Result<Guid>.Failure("Transaction not found");
        if (transaction.Status != TransactionStatus.Completed)
            return Result<Guid>.Failure("Can only refund completed transactions");

        var refund = Refund.Create(transaction.Id, transaction.OrderId, transaction.Amount, request.Reason);

        // Simulate gateway refund
        refund.MarkProcessed($"ref_{Guid.NewGuid():N}");

        refundRepo.Add(refund);
        transaction.MarkRefunded();
        await uow.SaveChangesAsync(ct);

        return Result<Guid>.Success(refund.Id);
    }
}
```

### 7. Add Refund Endpoint
**File:** `src/Microservices/Payment/Payment.API/Endpoints/PaymentEndpoints.cs`

```csharp
group.MapPost("/{transactionId:guid}/refund", async (
    Guid transactionId,
    [FromBody] RefundRequest request,
    [FromServices] ISender sender,
    CancellationToken ct) =>
{
    var result = await sender.Send(new RefundPaymentCommand(transactionId, request.Reason), ct);
    return result.IsSuccess
        ? Results.Created($"/api/payments/refund/{result.Value}", new { refundId = result.Value })
        : Results.BadRequest(new { result.Error });
}).RequireAuthorization("Admin");
```

### 8. Update PaymentEndpoints GET to Include Refunds
Update `GET /api/payments/order/{orderId}` to also return refund records.

### 9. Add MarkRefunded to PaymentTransaction
**File:** `src/Microservices/Payment/Payment.Domain/Aggregates/PaymentTransaction.cs`

```csharp
public void MarkRefunded()
{
    Status = TransactionStatus.Refunded;
}
```

## E2E Verification

### Spec File: `tests/E2ETests/tests/payment-refund.spec.ts`

**Scenario:** Buyer places order. Admin triggers refund. Refund record appears.

```
TEST: payment-refund.spec.ts

Setup:
  1. Register buyer via API
  2. Create store + product via seller API
  3. Login as buyer → add to cart → checkout → place order
  4. Wait for order completion
  5. Get order ID and payment transaction ID via API

Test: "admin can refund a completed payment"
  6. Login as admin in browser
  7. Navigate to admin panel
  8. (If admin refund UI exists) trigger refund
  9. (Otherwise) call POST /api/payments/{transactionId}/refund via API
  10. Verify 201 response with refundId

Test: "refund record appears in payment query"
  11. GET /api/payments/order/{orderId} via API
  12. Verify response includes a refund record
  13. Verify refund status is "Processed"
  14. Verify refund amount equals transaction amount

Test: "cannot refund already refunded transaction"
  15. POST /api/payments/{transactionId}/refund again
  16. Verify 400 response ("already refunded")

Test: "buyer sees refunded status in order detail"
  17. Login as buyer in browser
  18. Navigate to /orders → order detail
  19. Verify payment status shows "Refunded"
```

### New Page Objects
- None — uses API calls for refund triggering

### Files to Create/Modify
```
tests/E2ETests/tests/payment-refund.spec.ts               # NEW
```

## Acceptance Criteria
- [ ] `Refund` aggregate exists in Payment.Domain
- [ ] `RefundRepository` implements `IRefundRepository`
- [ ] `PaymentDbContext` has `DbSet<Refund>`
- [ ] `RefundPaymentHandler` creates refund, marks transaction as refunded
- [ ] `POST /api/payments/{transactionId}/refund` endpoint works (Admin only)
- [ ] `GET /api/payments/order/{orderId}` includes refund records
- [ ] `PaymentTransaction.MarkRefunded()` sets status
- [ ] E2E test passes: admin refunds → refund record visible → cannot double-refund
- [ ] All existing tests still pass

## Verification Commands
```bash
dotnet build Marketplace.slnx
dotnet test tests/UnitTests/Payment.UnitTests/ --no-build
npx playwright test tests/E2ETests/tests/payment-refund.spec.ts
```
