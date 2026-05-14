# Identity Service — Integration Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Identity/`

## Goal
Implement integration tests for the Identity service using Testcontainers for PostgreSQL, testing the actual database repository logic.

## Scope
- **In**: `UserRepository` EF Core queries/saves, database constraints, DB migrations.
- **Out**: Unit logic, external frontend clients.

## Action Items

[ ] **Step 1: Set up Project**
  - Verify/Create `tests/IntegrationTests/Identity.IntegrationTests` referencing `Identity.Infrastructure`.
  - Install packages: `xunit`, `FluentAssertions`, `Testcontainers.PostgreSql`, `Microsoft.EntityFrameworkCore`.

[ ] **Step 2: Test Fixture Setup**
  - Create `IdentityDatabaseFixture` using `PostgreSqlContainer` (image: `postgres:16-alpine`).
  - Configure `IAsyncLifetime` to start container and apply migrations to `IdentityDbContext`.

[ ] **Step 3: Repository Tests (Write)**
  - Test: `AddAsync` saves a new User to the PostgreSQL database successfully.
  - Test: Saving a User with a duplicate Email violates database unique constraint.

[ ] **Step 4: Repository Tests (Read)**
  - Test: `GetByEmailAsync` retrieves correct User tracking state.
  - Test: `GetByEmailAsync` returns null for non-existent email.

[ ] **Step 5: Validation**
  - Run `dotnet test tests/IntegrationTests/Identity.IntegrationTests/Identity.IntegrationTests.csproj`.
  - Ensure Testcontainers correctly spin up, run tests, and tear down.