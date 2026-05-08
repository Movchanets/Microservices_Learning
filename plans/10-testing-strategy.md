# 10 — Testing Strategy

## Three-Level Testing Pyramid

```
        ┌─────────┐
        │  E2E    │  Playwright (browser)
        │ (few)   │
       ┌┴─────────┴┐
       │Integration │  Testcontainers (real DBs)
       │  (more)    │
      ┌┴────────────┴┐
      │  Unit Tests   │  xUnit + Moq + FluentAssertions
      │  (most)       │
      └───────────────┘
```

## 1. Unit Tests

**Target**: Domain and Application layers (80%+ coverage)

| Tool | Purpose |
|:---|:---|
| xUnit | Test framework |
| Moq | Mocking dependencies |
| FluentAssertions | Readable assertions |

**What to test:**
- Aggregate invariants and domain rules
- Command/Query handlers (mock repositories)
- Value Object equality and validation
- Domain Event generation

```csharp
[Fact]
public void Order_AddItem_WithNegativePrice_ShouldThrow()
{
    var order = Order.Create("buyer-1");
    var act = () => order.AddItem("sku-1", price: -10, quantity: 1);
    act.Should().Throw<DomainException>()
       .WithMessage("*price*");
}
```

## 2. Integration Tests (Testcontainers)

**Target**: Infrastructure layer — real databases, real brokers

| Container | Tests |
|:---|:---|
| PostgreSQL | EF Core migrations, LINQ queries, repository behavior |
| Redis | Cart operations, cache hit/miss |
| RabbitMQ | MassTransit consumer delivery, saga state transitions |

```csharp
public class OrderRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Apply migrations, seed data
    }

    [Fact]
    public async Task GetById_ExistingOrder_ReturnsOrder()
    {
        // Arrange — real PostgreSQL
        using var context = CreateDbContext(_postgres.GetConnectionString());
        var repo = new OrderRepository(context);
        // Act & Assert...
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
```

## 3. E2E Tests (Playwright)

**Target**: Full user flows through the browser → YARP → microservices → database

**Scenarios:**
- BFF cookie authentication flow (login → session cookie → API access)
- Product search and catalog browsing
- Cart → Checkout → Order confirmation
- Real-time SignalR notifications (order status push)
- Seller dashboard and product management

```typescript
test('buyer can complete checkout', async ({ page }) => {
    await page.goto('/login');
    await page.fill('[data-testid="email"]', 'buyer@test.com');
    await page.fill('[data-testid="password"]', 'P@ssw0rd');
    await page.click('[data-testid="login-btn"]');

    // Add to cart
    await page.goto('/products');
    await page.click('[data-testid="add-to-cart-1"]');
    await page.goto('/cart');
    await page.click('[data-testid="checkout-btn"]');

    // Verify order confirmation
    await expect(page.locator('[data-testid="order-confirmed"]'))
        .toBeVisible({ timeout: 15000 });
});
```

## Test Directory Structure

```
tests/
├── UnitTests/
│   ├── Catalog.Domain.Tests/
│   ├── Ordering.Domain.Tests/
│   ├── Ordering.Application.Tests/
│   └── Inventory.Domain.Tests/
├── IntegrationTests/
│   ├── Catalog.IntegrationTests/
│   ├── Ordering.IntegrationTests/
│   └── Identity.IntegrationTests/
└── E2ETests/
    ├── playwright.config.ts
    ├── tests/
    │   ├── auth.spec.ts
    │   ├── catalog.spec.ts
    │   └── checkout.spec.ts
    └── pages/               # Page Object Models
```

## Quality Gates

- [ ] 80%+ code coverage on Domain logic
- [ ] All integration tests pass with Testcontainers
- [ ] E2E tests pass across Chromium, Firefox
- [ ] No regressions in CI pipeline before merge
