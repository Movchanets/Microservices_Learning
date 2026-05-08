# Tests

This folder contains the Marketplace test pyramid:

- **Unit tests**: `tests/UnitTests`
- **Integration tests**: `tests/IntegrationTests`
- **E2E tests**: `tests/E2ETests`

## Test stack

| Tool | Usage |
|:---|:---|
| xUnit | Primary test framework for .NET tests |
| Moq | Mocking dependencies in unit tests |
| FluentAssertions | Readable, expressive assertions |
| Microsoft.NET.Test.Sdk | Test runner integration for `dotnet test` |
| coverlet.collector | Code coverage collection |
| Testcontainers | Real infrastructure in integration tests (PostgreSQL, RabbitMQ, Redis) |
| Playwright | End-to-end browser tests |

## Naming conventions

- Unit tests: `{Service}.UnitTests`
- Integration tests: `{Service}.IntegrationTests`

## Shared integration fixtures

The `tests/IntegrationTests/Shared` folder is reserved for reusable integration test fixtures/helpers (for example: container factories, migration helpers, in-memory auth/test server helpers).

All integration test projects should consume shared fixtures from this folder instead of duplicating fixture logic.

IntegrationTests must use the shared PostgreSQL helpers (`PostgresContainerFactory` and `ApplyMigrationsAsync`) from `tests/IntegrationTests/Shared`.
