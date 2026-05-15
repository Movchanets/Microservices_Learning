# Task: Implement Identity Service Integration Tests

**Source Plan**: `implementation_plan/tests/identity.integration_tests.md`

Goal: Implement integration tests for the Identity service using Testcontainers for PostgreSQL to test actual repository logic.

Context: 
- Location: src/Microservices/Identity/
- Target Project: tests/IntegrationTests/Identity.IntegrationTests
- References: Identity.Infrastructure

Action Items:
1. Project Setup:
   - Verify/Create tests/IntegrationTests/Identity.IntegrationTests.
   - Install NuGet packages: xunit, FluentAssertions, Testcontainers.PostgreSql, Microsoft.EntityFrameworkCore.
2. Test Fixture Setup:
   - Create IdentityDatabaseFixture using PostgreSqlContainer (image: postgres:16-alpine).
   - Implement IAsyncLifetime to start container and apply migrations to IdentityDbContext.
3. Repository Tests (Write/Read):
   - Test: AddAsync saves a new User successfully.
   - Test: Duplicate Email violations (database unique constraint).
   - Test: GetByEmailAsync retrieves correct User tracking state.
   - Test: GetByEmailAsync returns null for non-existent email.

Validation:
- Run: dotnet test tests/IntegrationTests/Identity.IntegrationTests/Identity.IntegrationTests.csproj
- Ensure Testcontainers spin up, run, and tear down correctly.
