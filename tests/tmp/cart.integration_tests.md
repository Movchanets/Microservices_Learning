# Task: Implement Cart Service Integration Tests

**Source Plan**: `implementation_plan/tests/cart.integration_tests.md`

Goal: Implement integration tests for the Cart service testing hybrid PostgreSQL + Redis repository logic via Testcontainers.

Context: 
- Location: src/Microservices/Cart/
- Target Project: tests/IntegrationTests/Cart.IntegrationTests
- References: Cart.Infrastructure

Action Items:
1. Project Setup:
   - Verify/Create tests/IntegrationTests/Cart.IntegrationTests.
   - Install NuGet packages: xunit, FluentAssertions, Testcontainers.PostgreSql, Testcontainers.Redis.
2. Test Fixture Setup:
   - Create CartDatabaseFixture utilizing BOTH PostgreSqlContainer and RedisContainer.
   - Apply migrations and configure IDistributedCache targeting the Redis container.
3. Hybrid Repository Tests:
   - Test: GetCartAsync (Cache Miss): retrieves from DB, sets in Redis.
   - Test: GetCartAsync (Cache Hit): retrieves directly from Redis.
   - Test: UpdateCartAsync persists to PostgreSQL and updates Redis simultaneously.
   - Test: DeleteCartAsync drops records from both stores.

Validation:
- Run: dotnet test tests/IntegrationTests/Cart.IntegrationTests/Cart.IntegrationTests.csproj
