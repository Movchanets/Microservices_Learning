# Payment Service — Unit Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Payment/`

## Goal
Implement unit tests for the Payment domain and application layers using xUnit, Moq, and FluentAssertions.

## Scope
- **In**: `PaymentTransaction` aggregate, MediatR handlers, `MockPaymentGateway`.
- **Out**: PostgreSQL querying, MassTransit message dispatch.

## Action Items

[ ] **Step 1: Set up Project**
  - Create `tests/UnitTests/Payment.UnitTests` referencing `Payment.Domain` and `Payment.Application`.
  - Install packages: `xunit`, `Moq`, `FluentAssertions`.

[ ] **Step 2: Domain Layer Tests (`PaymentTransaction` Aggregate)**
  - Test: `PaymentTransaction.Create` initializes with `Pending` status, correct `OrderId`, `BuyerId`, `Amount`, `CreatedAt`.
  - Test: `PaymentTransaction.Create` throws when `Amount` <= 0.
  - Test: `PaymentTransaction.Create` throws when `BuyerId` is empty.
  - Test: `MarkCompleted` sets `Status` to `Completed`, stores `TransactionId`, sets `ProcessedAt`.
  - Test: `MarkFailed` sets `Status` to `Failed`, stores `FailureReason`, sets `ProcessedAt`.

[ ] **Step 3: Application Layer Tests (`ProcessPaymentHandler`)**
  - Test: Handler creates transaction, calls mock gateway, marks as completed, persists.
  - Test: Handler returns `Result<bool>.Success(true)` on successful payment.

[ ] **Step 4: MockPaymentGateway Tests**
  - Test: `MockPaymentGateway.ProcessPaymentAsync` always returns success.
  - Test: Result contains a `TransactionId` starting with `txn_`.

[ ] **Step 5: Validation**
  - Run `dotnet test tests/UnitTests/Payment.UnitTests/Payment.UnitTests.csproj`.
