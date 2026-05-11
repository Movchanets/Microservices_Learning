# AI Agent Instructions: Enterprise Marketplace Microservices

This document provides **strict, non-negotiable guidelines** for AI agents (Antigravity, Copilot, Codex) working on the Marketplace project. All agents MUST adhere to these rules. Violations break architectural integrity.

> **Before starting ANY work**, read the sub-plan referenced in the Trello card description (`📄 implementation_plan/phase-X/X.Y-*.md`). Execute the steps as documented — do not improvise the structure.

---

## 1. Project Context & Architecture

| Layer | Technology | Version |
|:---|:---|:---|
| Runtime | .NET | 10 |
| Language | C# | 14.1 |
| Orchestration | .NET Aspire | Latest |
| Frontend | Angular | 19+ |
| State | NgRx SignalStore | Signals-based |
| UI | Spartan/UI + Tailwind CSS | Latest |
| ORM | Entity Framework Core | 10 |
| CQRS | MediatR | Latest |
| Messaging | MassTransit (RabbitMQ / Azure Service Bus) | Latest |
| Gateway | YARP (BFF pattern) | Latest |
| Real-time | SignalR + Redis Backplane | Latest |
| Infrastructure | Azure Container Apps (ACA) | Via Aspirate/Terraform |

**Architecture**: Microservices · Database-per-Service · DDD · Clean Architecture · CQRS

---

## 2. Documentation Map

Agents MUST consult these docs before making decisions:

| Document | Path | Purpose |
|:---|:---|:---|
| Architecture Plans | `plans/` | System design, domain decomposition, patterns |
| Implementation Plan | `implementation_plan/` | Phased execution with sub-plans |
| Phase Sub-Plans | `implementation_plan/phase-X/` | Copy-paste code blocks for each task |
| Monorepo Layout | `plans/04-monorepo-structure.md` | Directory rules, BuildingBlocks conventions |
| Clean Architecture | `plans/03-clean-architecture.md` | Layer dependencies, code templates |
| Messaging & Sagas | `plans/05-messaging-sagas.md` | MassTransit patterns, Outbox, compensation |
| API Gateway & BFF | `plans/06-api-gateway-bff.md` | YARP routes, cookie-to-bearer flow |
| Security | `plans/08-security.md` | Zero Trust, CSRF, Managed Identities |
| Frontend Angular | `plans/11-frontend-angular.md` | Signals, SignalStore, project structure |
| C4 Diagrams | `plans/12-c4-diagrams.md` | Visual architecture reference |

---

## 3. Monorepo Structure

```
d:\code\Microservices\
├── Marketplace.sln
├── global.json                          # Pins .NET 10 SDK
├── AGENTS.md                            # THIS FILE
├── plans/                               # Architecture documentation
├── implementation_plan/                 # Phased task plans
├── src/
│   ├── Aspire/
│   │   ├── Marketplace.AppHost/         # Orchestration
│   │   └── Marketplace.ServiceDefaults/ # Shared telemetry, health, resilience
│   ├── Gateways/
│   │   └── ApiGateway/                  # YARP + BFF
│   ├── Microservices/
│   │   ├── Identity/                    # 4-layer Clean Architecture
│   │   ├── Catalog/
│   │   ├── Search/                      # Thin (Elasticsearch)
│   │   ├── Inventory/
│   │   ├── Cart/                        # Thin (Redis)
│   │   ├── Ordering/                    # Saga State Machine
│   │   ├── Payment/
│   │   ├── StoreManagement/
│   │   ├── Media/                       # Thin (Blob Storage)
│   │   └── Notification/               # Worker (SignalR)
│   ├── BuildingBlocks/
│   │   ├── SharedContracts/             # DDD base types + integration events
│   │   └── Infrastructure/             # Cross-cutting middleware, behaviors
│   └── web/                             # Angular SPA
└── tests/
    ├── UnitTests/
    ├── IntegrationTests/
    └── E2ETests/
```

---

## 4. Workflow: Trello Task Management

Every development step MUST be tracked in Trello.

### Process
1. **Before coding**: Move the Trello card from **To Do** → **In Progress**
2. **During coding**: Log blockers or design decisions as comments on the card
3. **After coding**: Move to **Review** (if PR needed) or **Done**

### Trello Configuration
- **Board ID**: `69f8a84ab3c95b66da809269`
- **Board URL**: https://trello.com/b/qUl75p1Q

| List | ID |
|:---|:---|
| Backlog | `69f8a892c1356567cb2f2730` |
| To Do | `69f8a8942b982614c89874d6` |
| In Progress | `69f8a89487c004e5c4a53fe2` |
| Review | `69f8a8953c1f85aa1b611aac` |
| Done | `69f8a89556cc4f8584cfa103` |

### Card Format
Every card description MUST include:
- `📄 Sub-Plan` — path to the implementation sub-plan file
- `📐 Architecture Reference` — path to the relevant `plans/` doc
- `Acceptance Criteria` — checkbox list of verifiable outcomes

---

## 5. Context Management: Graphify

To maintain a consistent mental model of the distributed system, agents must use **Graphify**.
- **Graph Updates**: After creating a new microservice, integration event, or saga, update the knowledge graph.
- **Dependency Mapping**: Ensure all S2S communications (HTTP or Messaging) are reflected in the graph.
- **Context Retrieval**: Use Graphify to understand the impact of changes on downstream services before proposing refactors.

---

## 6. Backend Development Rules (.NET)

### 6.1 Clean Architecture — MANDATORY

Every microservice with business logic follows 4 layers. **Never shortcut this.**

```
{Service}.Domain/        → Aggregates, ValueObjects, Events, Enums (ZERO dependencies)
{Service}.Application/   → Commands, Queries, Handlers, Validators, DTOs, Interfaces
{Service}.Infrastructure/→ EF Core, Repos, Consumers, External Clients
{Service}.API/           → Minimal API Endpoints, Program.cs
```

**Thin services** (Cart, Search, Media) may skip Domain/Application layers.

### 6.2 C# 14.1 & .NET 10 Patterns

```csharp
// ✅ Primary constructors for DI
public sealed class CreateOrderHandler(
    IOrderRepository repository,
    IUnitOfWork uow)
    : IRequestHandler<CreateOrderCommand, Result<Guid>>

// ✅ Use `field` keyword for validated properties
public string BuyerId
{
    get => field;
    init => field = !string.IsNullOrWhiteSpace(value)
        ? value : throw new DomainException("BuyerId required");
}

// ✅ Collection expressions
private readonly List<OrderItem> _items = [];

// ✅ Records for DTOs and commands
public sealed record CreateOrderCommand(
    string BuyerId,
    List<OrderItemDto> Items) : IRequest<Result<Guid>>;
```

### 6.3 Minimal API Endpoints

```csharp
// ✅ CORRECT — Grouped endpoints, OpenAPI, MediatR
public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/", async (CreateOrderCommand cmd, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Created($"/api/orders/{result.Value}", result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        });
    }
}
```

**Rules**:
- NEVER put business logic in endpoints — delegate to MediatR handlers
- ALWAYS use `CancellationToken`
- ALWAYS tag with `.WithOpenApi()` and `.WithTags()`
- ALWAYS use `Results.*` return types, never throw from endpoints

### 6.4 Entity Framework Core

```csharp
// ✅ DbContext implements IUnitOfWork
public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
    }
}
```

**Rules**:
- Use `IEntityTypeConfiguration<T>` — NEVER configure in `OnModelCreating` directly
- Use value conversions for Value Objects
- Use owned types for nested VOs (e.g., `RefreshToken`)
- `AsNoTracking()` for all read queries
- Each service has its own isolated database — NEVER share a DbContext

### 6.5 MediatR Pipeline

Every microservice `Program.cs` MUST register:

```csharp
// MediatR with pipeline behaviors
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof({ServiceName}Command).Assembly));

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof({ServiceName}Validator).Assembly);
```

Execution order: **Validation → Logging → Handler**

### 6.6 Service Program.cs Template

Every microservice `Program.cs` MUST follow this structure:

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Aspire ServiceDefaults (telemetry, health, resilience)
builder.AddServiceDefaults();

// 2. Aspire resource integrations (database, messaging)
builder.AddNpgsqlDbContext<ServiceDbContext>("service-db");

// 3. Service-specific infrastructure (repos, services)
builder.Services.AddServiceInfrastructure(builder.Configuration);

// 4. MediatR + behaviors + validators
// 5. Authentication (JWT Bearer)
// 6. Authorization
// 7. OpenAPI

var app = builder.Build();

// Middleware pipeline:
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapDefaultEndpoints(); // health checks
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();

// Endpoints
app.Map{ServiceName}Endpoints();

app.Run();
```

### 6.7 Messaging & Sagas

- **Outbox Pattern**: ALWAYS use MassTransit Outbox for guaranteed delivery
- **Sagas**: Orchestrate multi-service flows using `MassTransitStateMachine`
- **Contracts**: ALL integration events/commands in `BuildingBlocks.SharedContracts/Events/` and `Commands/`
- **Consumer naming**: `{EventName}Consumer` (e.g., `OrderSubmittedConsumer`)
- **Compensation**: Every saga MUST define rollback paths for failures

```csharp
// ✅ Integration event in SharedContracts
public record OrderSubmittedEvent(
    Guid CorrelationId,
    string BuyerId,
    List<OrderItemContract> Items,
    DateTime Timestamp);
```

### 6.8 BuildingBlocks Rules

| ✅ ALLOWED | ❌ FORBIDDEN |
|:---|:---|
| Integration event records | Shared domain entities |
| DDD base types (AggregateRoot, Entity, ValueObject) | EF Core / Npgsql packages |
| Cross-cutting middleware | Business logic / domain rules |
| Pipeline behaviors | Discount calculations, permission checks |
| `IRepository<T>`, `IUnitOfWork` | Any ORM dependency |

> BuildingBlocks = **infrastructure glue + contracts**, NOT a central business library.

### 6.9 Aspire AppHost

Every new service MUST be registered in `src/Aspire/Marketplace.AppHost/Program.cs`:

```csharp
var serviceApi = builder.AddProject<Projects.Service_API>("service-api")
    .WithReference(serviceDb)
    .WithReference(messaging);
```

Every new service MUST reference `Marketplace.ServiceDefaults` and call:
```csharp
builder.AddServiceDefaults();   // in ConfigureServices
app.MapDefaultEndpoints();      // in pipeline
```

---

## 7. Frontend Development Rules (Angular)

### 7.1 Core Principles

| Rule | Description |
|:---|:---|
| **No NgModules** | All components `standalone: true` |
| **Signals first** | `signal()`, `computed()`, `input()`, `output()` — no RxJS for local state |
| **OnPush always** | `changeDetection: ChangeDetectionStrategy.OnPush` on every component |
| **Lazy loading** | Route-level code splitting via `loadComponent` / `loadChildren` |
| **BFF only** | All API calls through YARP with `withCredentials: true` |
| **Zoneless ready** | Prepare for `provideZonelessChangeDetection()` |

### 7.2 Component Template

```typescript
// ✅ CORRECT — Every new component MUST follow this pattern
@Component({
  selector: 'app-example',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [/* only what's used */],
  template: `
    @if (loading()) {
      <app-skeleton />
    } @else {
      @for (item of items(); track item.id) {
        <app-item-card [item]="item" />
      } @empty {
        <p>No items found</p>
      }
    }
  `
})
export class ExampleComponent {
  private store = inject(ExampleStore);

  loading = this.store.loading;
  items = this.store.filteredItems;
}
```

### 7.3 State Management — NgRx SignalStore

```typescript
// ✅ Feature-scoped store (NOT providedIn: 'root' for feature stores)
export const CatalogStore = signalStore(
  withState<CatalogState>({ products: [], loading: false, searchQuery: '' }),
  withComputed((store) => ({
    filteredProducts: computed(() =>
      store.products().filter(p =>
        p.name.toLowerCase().includes(store.searchQuery().toLowerCase())
      )
    ),
  })),
  withMethods((store, catalogService = inject(CatalogService)) => ({
    async loadProducts(): Promise<void> {
      patchState(store, { loading: true });
      const products = await catalogService.getAll();
      patchState(store, { products, loading: false });
    },
  }))
);
```

### 7.4 Template Rules

```html
<!-- ✅ New control flow (MANDATORY) -->
@if (condition()) { } @else { }
@for (item of items(); track item.id) { } @empty { }
@switch (status()) { @case ('active') { } }

<!-- ❌ FORBIDDEN — Legacy directives -->
*ngIf, *ngFor, *ngSwitch, [ngSwitch]
```

### 7.5 Performance Rules

| Pattern | Rule |
|:---|:---|
| Template methods | ❌ NEVER use methods in templates — use `computed()` or pure pipes |
| Large lists | ✅ ALWAYS use `CdkVirtualScrollViewport` for 50+ items |
| Heavy components | ✅ Use `@defer (on viewport)` for below-fold content |
| Third-party libs | ✅ Dynamic `import()` — never in main bundle |
| Barrel re-exports | ❌ AVOID — use direct imports for tree-shaking |
| Subscriptions | ✅ Use `takeUntilDestroyed()` — or prefer `toSignal()` |

### 7.6 HTTP & BFF Integration

```typescript
// ✅ ALL API calls go through the BFF — never directly to microservices
export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const apiReq = req.clone({ withCredentials: true });
  return next(apiReq);
};
```

### 7.7 SignalR Integration

```typescript
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private hubConnection = new HubConnectionBuilder()
    .withUrl('/hubs/notifications', { withCredentials: true })
    .withAutomaticReconnect()
    .build();

  readonly orderUpdates = signal<OrderUpdate | null>(null);

  async start(): Promise<void> {
    this.hubConnection.on('OrderUpdate', (update: OrderUpdate) => {
      this.orderUpdates.set(update);
    });
    await this.hubConnection.start();
  }
}
```

### 7.8 Project Structure

```
src/web/src/app/
├── core/           # Singleton services, guards, interceptors (providedIn: 'root')
│   ├── auth/
│   ├── http/
│   └── signalr/
├── features/       # Lazy-loaded routes, each with its own SignalStore
│   ├── catalog/
│   ├── cart/
│   ├── checkout/
│   ├── orders/
│   ├── seller-dashboard/
│   └── admin/
├── shared/         # Reusable components, pipes, directives
├── app.component.ts
├── app.config.ts
└── app.routes.ts
```

---

## 8. Verification Protocol

### 8.1 Before Every Commit

| Check | Command |
|:---|:---|
| Solution builds | `dotnet build Marketplace.sln` |
| Unit tests pass | `dotnet test tests/UnitTests/` |
| No lint errors | `ng lint` (frontend) |

### 8.2 Quality Gates

| Level | Tool | Requirement |
|:---|:---|:---|
| Unit | xUnit + Moq + FluentAssertions | 80%+ coverage on Domain logic |
| Integration | Testcontainers (PostgreSQL, RabbitMQ, Redis) | All consumers and repos tested against real infra |
| E2E | Playwright | BFF cookie flow, checkout journey, SignalR notifications |

### 8.3 Test Naming Convention

```csharp
// Unit tests
[Fact]
public void Create_WithValidData_ReturnsUser()
[Fact]
public void Create_WithEmptyEmail_ThrowsArgumentException()

// Integration tests
[Fact]
public async Task Register_WithNewEmail_PersistsUserToDatabase()
```

### 8.4 Test Directory

```
tests/
├── UnitTests/
│   ├── Catalog.Domain.Tests/
│   ├── Ordering.Domain.Tests/
│   └── Inventory.Domain.Tests/
├── IntegrationTests/
│   ├── Identity.IntegrationTests/
│   ├── Catalog.IntegrationTests/
│   └── Ordering.IntegrationTests/
└── E2ETests/
    ├── playwright.config.ts
    ├── tests/
    └── pages/           # Page Object Models
```

---

## 9. Git Conventions

### Commit Messages

```
feat: Phase X.Y - <description>
fix: Resolve <issue> in <service>
refactor: Extract <pattern> in <service>
test: Add integration tests for <consumer>
docs: Update <plan document>
```

### Branch Naming

```
feature/phase-X-<short-description>
fix/<trello-card-id>-<short-description>
```


<!-- nx configuration start-->
<!-- Leave the start & end comments to automatically receive updates. -->

## General Guidelines for working with Nx

- For navigating/exploring the workspace, invoke the `nx-workspace` skill first - it has patterns for querying projects, targets, and dependencies
- When running tasks (for example build, lint, test, e2e, etc.), always prefer running the task through `nx` (i.e. `nx run`, `nx run-many`, `nx affected`) instead of using the underlying tooling directly
- Prefix nx commands with the workspace's package manager (e.g., `pnpm nx build`, `npm exec nx test`) - avoids using globally installed CLI
- You have access to the Nx MCP server and its tools, use them to help the user
- For Nx plugin best practices, check `node_modules/@nx/<plugin>/PLUGIN.md`. Not all plugins have this file - proceed without it if unavailable.
- NEVER guess CLI flags - always check nx_docs or `--help` first when unsure

## Scaffolding & Generators

- For scaffolding tasks (creating apps, libs, project structure, setup), ALWAYS invoke the `nx-generate` skill FIRST before exploring or calling MCP tools

## When to use nx_docs

- USE for: advanced config options, unfamiliar flags, migration guides, plugin configuration, edge cases
- DON'T USE for: basic generator syntax (`nx g @nx/react:app`), standard commands, things you already know
- The `nx-generate` skill handles generator discovery internally - don't call nx_docs just to look up generator syntax


<!-- nx configuration end-->