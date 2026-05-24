# Payment Service — Integration Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Payment/`

## Goal
Implement integration tests for the Payment service testing PostgreSQL repository, EF Core persistence, and the `ProcessPaymentConsumer` using Testcontainers.

## Scope
- **In**: `PaymentTransactionRepository`, `PaymentDbContext`, `ProcessPaymentConsumer`, `MockPaymentGateway`.
- **Out**: UI testing, real payment gateway integration.

## Action Items

[ ] **Step 1: Set up Project**
  - Create `tests/IntegrationTests/Payment.IntegrationTests` referencing `Payment.Infrastructure` and `Payment.API`.
  - Install packages: `xunit`, `FluentAssertions`, `Testcontainers.PostgreSql`, `MassTransit.TestFramework`.

[ ] **Step 2: Test Fixture Setup**
  - Create `PaymentDatabaseFixture` utilizing `PostgreSqlContainer`.
  - Apply `PaymentDbContext` schema on startup (including Outbox tables).

[ ] **Step 3: Repository Tests**
  - Test: Save and retrieve a `PaymentTransaction` — verify all fields persisted.
  - Test: `GetByOrderIdAsync` returns the transaction for the specified order.
  - Test: `GetByOrderIdAsync` returns null for non-existent order.
  - Test: Update a transaction status (Pending → Completed) and verify persistence.

[ ] **Step 4: Consumer Integration Tests**
  - Test: `ProcessPaymentConsumer` consumes `ProcessPaymentCommand`, creates transaction, publishes `PaymentCompletedEvent`.
  - Test: `ProcessPaymentConsumer` persists transaction to database with correct `OrderId`, `Amount`, `Status=Completed`.
  - Test: Transaction has valid `TransactionId` (from mock gateway).

[ ] **Step 5: Validation**
  - Run `dotnet test tests/IntegrationTests/Payment.IntegrationTests/Payment.IntegrationTests.csproj`.
