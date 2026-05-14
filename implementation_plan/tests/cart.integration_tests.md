# Cart Service — Integration Tests Plan

> **Ref**: [plans/10-testing-strategy.md](../../plans/10-testing-strategy.md) · `src/Microservices/Cart/`

## Goal
Implement integration tests for the Cart service testing hybrid PostgreSQL + Redis repository logic via Testcontainers.

## Scope
- **In**: `CartRepository`, `CartDbContext`, Distributed Cache interactions.
- **Out**: External APIs.

## Action Items

[ ] **Step 1: Set up Project**
  - Verify/Create `tests/IntegrationTests/Cart.IntegrationTests` referencing `Cart.Infrastructure`.
  - Install packages: `xunit`, `FluentAssertions`, `Testcontainers.PostgreSql`, `Testcontainers.Redis`.

[ ] **Step 2: Test Fixture Setup**
  - Create `CartDatabaseFixture` utilizing BOTH `PostgreSqlContainer` and `RedisContainer`.
  - Apply `CartDbContext` migrations on startup and configure `IDistributedCache` targeting the Redis container.

[ ] **Step 3: Hybrid Repository Tests**
  - Test: `GetCartAsync` retrieves cart from DB and sets it in Redis (cache miss).
  - Test: `GetCartAsync` retrieves cart directly from Redis (cache hit).
  - Test: `UpdateCartAsync` persists items correctly to PostgreSQL and updates the Redis cache simultaneously.
  - Test: `DeleteCartAsync` successfully drops records from PostgreSQL and removes the Redis key.

[ ] **Step 4: Validation**
  - Run `dotnet test tests/IntegrationTests/Cart.IntegrationTests/Cart.IntegrationTests.csproj`.