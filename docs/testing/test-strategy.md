# Test Strategy

**Project:** Marketplace Microservices
**Last Updated:** 2026-06-19

---

## Testing Pyramid

```
        ╱  E2E  ╲          ~63 tests (Playwright)
       ╱─────────╲         Smoke + critical paths
      ╱ Contract   ╲        51 tests (xUnit)
     ╱───────────────╲      API contract validation
    ╱  Integration     ╲     51 tests (xUnit + Testcontainers)
   ╱─────────────────────╲   DB + message broker
  ╱     Unit Tests        ╲  576 tests (xUnit + Vitest)
 ╱─────────────────────────╲ Fast, isolated, comprehensive
```

| Layer | Count | Tools | Runs In | Speed |
|-------|-------|-------|---------|-------|
| Backend Unit | 239 | xUnit, Moq, FluentAssertions | CI/local | <10s |
| Backend Integration | 51 | xUnit, Testcontainers | CI/local | ~60s |
| Backend Contract | 51 | xUnit | CI/local | <15s |
| Frontend Unit | 337 | Vitest, Angular TestBed | CI/local | <30s |
| E2E | ~63 | Playwright | CI (staging) | ~120s |
| **Total** | **~741** | | | |

---

## Tools & Frameworks

### Backend (.NET 10)

| Tool | Purpose | Config |
|------|---------|--------|
| **xUnit** | Test framework | `*.csproj` test projects |
| **Moq** | Mocking | Interface mocks for handlers |
| **FluentAssertions** | Assertions | `.Should().Be()`, `.Should().HaveCount()` |
| **Testcontainers** | Integration DB | PostgreSQL, Redis containers |
| **WebApplicationFactory** | API integration | In-process test server |
| **Coverlet** | Code coverage | `--collect:"XPlat Code Coverage"` |
| **Bogus** | Test data | Realistic fake data generation |

### Frontend (Angular 21)

| Tool | Purpose | Config |
|------|---------|--------|
| **Vitest** | Test runner | `vite.config.ts` (replaces Karma) |
| **Angular TestBed** | Component testing | `TestBed.configureTestingModule` |
| **jsdom** | DOM environment | Default Vitest environment |

### E2E

| Tool | Purpose | Config |
|------|---------|--------|
| **Playwright** | Browser automation | `playwright.config.ts` |
| **Page Object Model** | Test structure | `tests/E2ETests/pages/` |
| **Auth Fixtures** | Pre-authenticated contexts | `tests/E2ETests/fixtures/auth.fixture.ts` |
| **API Helpers** | Test data setup | `tests/E2ETests/utils/api-helpers.ts` |

---

## Test Conventions

### Backend Unit Tests

```
tests/UnitTests/{Service}.UnitTests/
├── Domain/
│   └── {Entity}Tests.cs          # Entity behavior
├── Application/
│   └── {Command}HandlerTests.cs   # CQRS handler tests
└── Infrastructure/
    └── {Service}Tests.cs          # Infrastructure service tests
```

**Naming:** `{ClassUnderTest}Tests.cs`
**Pattern:** Arrange → Act → Assert
**Mocking:** Mock interfaces via Moq; never mock concrete classes
**Assertions:** FluentAssertions (`.Should().Be()`, `.Should().NotBeNull()`)

### Backend Integration Tests

```
tests/IntegrationTests/{Service}.IntegrationTests/
├── {Repository}Tests.cs           # Repository CRUD
├── Consumers/
│   └── {Event}ConsumerTests.cs    # MassTransit consumer tests
└── Fixtures/
    └── {Service}DatabaseFixture.cs # Testcontainers setup
```

**Database:** Each fixture creates a fresh PostgreSQL container per test class
**Isolation:** Tests run in parallel across classes, serial within a class
**Seeding:** Fixtures seed required reference data

### Backend Contract Tests

```
tests/ContractTests/Contracts/
└── {Producer}To{Consumer}ContractTests.cs
```

**Purpose:** Verify message contracts between services
**Pattern:** Serialize → Deserialize → Assert field mapping

### Frontend Unit Tests

```
src/web/src/app/{feature}/{component}/
├── component.ts
├── component.html
└── component.spec.ts              # Co-located test
```

**Naming:** `{component}.spec.ts` (co-located with source)
**Pattern:** `TestBed.configureTestingModule({ imports: [ComponentUnderTest] })`
**Mocking:** `{ provide: SomeService, useValue: mockService }`
**Store testing:** Direct store method calls, assert signal values

### E2E Tests

```
tests/E2ETests/tests/
├── {feature}/
│   └── {scenario}.spec.ts
├── fixtures/
│   └── auth.fixture.ts            # Pre-authenticated contexts
├── pages/
│   └── {feature}.page.ts          # Page Object Models
├── components/
│   └── {component}.component.ts   # Component Object Models
└── utils/
    ├── api-helpers.ts             # API setup helpers
    └── types.ts                   # Shared types
```

**Naming:** `{feature}-{scenario}.spec.ts`
**Pattern:** Page Object Model (POM)
**Auth:** Use `auth.fixture.ts` for pre-authenticated Buyer/Seller/Admin contexts
**Data setup:** Use API helpers to create test data before UI assertions
**Stability:** Use `expect(locator).toBeVisible()` (retries), never `isVisible()` + manual assert

---

## Test Data Strategy

| Layer | Strategy |
|-------|----------|
| Backend Unit | Moq mocks + inline test data |
| Backend Integration | Testcontainers + seeded fixtures |
| Backend Contract | Inline DTO construction |
| Frontend Unit | Mock services with `useValue` |
| E2E | API helpers create real data via HTTP |

---

## CI/CD Integration

### Pipeline Stages

```
┌─────────────┐    ┌─────────────────┐    ┌──────────────┐    ┌─────────┐
│ Build        │───▶│ Unit Tests       │───▶│ Integration   │───▶│ E2E     │
│ dotnet build │    │ dotnet test      │    │ Tests         │    │ Tests   │
│ pnpm build   │    │ pnpm test        │    │ (Docker req)  │    │ (Stg)   │
└─────────────┘    └─────────────────┘    └──────────────┘    └─────────┘
```

### Coverage Thresholds (Recommended)

| Layer | Target | Current |
|-------|--------|---------|
| Backend Unit | 80% line coverage | ~75% |
| Frontend Unit | 70% line coverage | ~65% |
| E2E Critical Paths | 100% of P0 flows | ~40% |

---

## Anti-Patterns to Avoid

| Anti-Pattern | Why It's Bad | Instead |
|-------------|-------------|---------|
| `test.skip()` for missing data | Masks real failures | Seed data in fixtures/setup |
| `waitForTimeout(ms)` | Flaky on slow CI | Use `expect(locator).toBeVisible({ timeout })` |
| Testing implementation details | Breaks on refactor | Test behavior, not internals |
| Shared mutable test state | Order-dependent failures | Fresh fixtures per test class |
| E2E tests for business logic | Slow, hard to debug | Use unit tests for logic |
| Mocking everything in integration | Defeats the purpose | Use real DB, mock only externals |

---

## Related Documentation

| Document | Path |
|----------|------|
| Coverage Summary | [coverage-summary.md](coverage-summary.md) |
| Backend Unit Tests | [backend-unit-tests.md](backend-unit-tests.md) |
| Backend Integration Tests | [backend-integration-tests.md](backend-integration-tests.md) |
| Frontend Tests | [frontend-tests.md](frontend-tests.md) |
| E2E Tests | [e2e-tests.md](e2e-tests.md) |

---

*This document defines the testing strategy for the Marketplace Microservices project.*
