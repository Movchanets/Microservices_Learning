# Identity Service — Unit Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Identity/`

## Goal
Implement unit tests for the Identity domain and application layers using xUnit, Moq, and FluentAssertions. Ensure >80% coverage on core logic.

## Scope
- **In**: Aggregate rules, value object validations, Command/Query handlers.
- **Out**: Database context testing, HTTP endpoints (handled in integration tests).

## Action Items

[ ] **Step 1: Set up Project**
  - Verify/Create `tests/UnitTests/Identity.UnitTests` referencing `Identity.Domain` and `Identity.Application`.
  - Ensure packages are installed: `xunit`, `Moq`, `FluentAssertions`.

[ ] **Step 2: Domain Layer Tests (`User` Aggregate)**
  - Test: User creation fails with empty/null email.
  - Test: User creation generates appropriate domain events.
  - Test: Updating user profile (Name, Address) works and preserves identity.
  - Test: Business rules or constraints on `Address` value object.

[ ] **Step 3: Application Layer Tests (Commands)**
  - Test: `RegisterUserCommandHandler` successfully registers user and commits.
  - Test: `RegisterUserCommandHandler` returns failure if email already exists (mocking repository).
  - Test: `LoginCommandHandler` returns successful `Result` and token upon correct credentials.

[ ] **Step 4: Application Layer Tests (Queries)**
  - Test: `GetUserByIdQueryHandler` returns user data when user exists.
  - Test: `GetUserByIdQueryHandler` returns null/failure when user does not exist.

[ ] **Step 5: Validation**
  - Run `dotnet test tests/UnitTests/Identity.UnitTests/Identity.UnitTests.csproj`.
  - Ensure all assertions pass and there are no warnings.