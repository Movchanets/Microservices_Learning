# Phase 6 — Research Findings

## Existing Patterns (from codebase analysis)

### Full Clean Architecture Reference (Catalog/Ordering)
- **4 layers**: Domain, Application, Infrastructure, API
- **Domain**: Aggregates extend `AggregateRoot` from SharedContracts, use C# 14 `field` keyword
- **Application**: MediatR Commands/Queries + FluentValidation validators + DTOs
- **Infrastructure**: DbContext implements `IUnitOfWork`, EF Core configs via `IEntityTypeConfiguration<T>`, `DatabaseMigrationExtensions.ApplyMigrations()`
- **API**: Minimal endpoints via `MapXxxEndpoints()`, JWT Bearer auth, `GlobalExceptionMiddleware`, `ValidationBehavior` + `LoggingBehavior` pipeline

### Thin Service Reference (Cart)
- Still uses 4 projects but simpler Domain (no domain events)
- Direct `AddNpgsqlDbContext<T>()` via Aspire
- MediatR without pipeline behaviors
- MassTransit registered but no Outbox

### Key Conventions
- **Aspire DB registration**: `builder.AddNpgsqlDbContext<TDbContext>("connection-name")`
- **DI pattern**: `DependencyInjection.cs` static class with `AddXxxInfrastructure()` extension method
- **DbContext → IUnitOfWork**: `services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<XxxDbContext>())`
- **Migration pattern**: `app.ApplyMigrations()` in dev, using `DatabaseMigrationExtensions`
- **JWT config**: Issuer "marketplace-identity", Audience "marketplace-api", Secret from env vars
- **Endpoints**: `.WithTags()`, `.WithOpenApi()`, `Results.*` returns

### AppHost State
- `storeDb` already defined: `postgres.AddDatabase("store-db")`
- Gateway already has `storeRoute` and `mediaRoute` + clusters configured
- StoreManagement.API and Media.API NOT yet registered in AppHost
- Scalar registrations needed for both

### Aspire Azure Storage Integration
- **AppHost**: `builder.AddAzureStorage("storage").AddBlobs("blobs")` — uses Azurite emulator locally
- **Client**: `builder.AddAzureBlobServiceClient("blobs")` → injects `BlobServiceClient`
- **Package**: `Aspire.Azure.Storage.Blobs` for client, `Aspire.Hosting.Azure.Storage` for AppHost
- **Health checks + telemetry**: Automatic via Aspire client integration

### Image Processing
- **SixLabors.ImageSharp** — cross-platform, no native deps, MIT license
- **SixLabors.ImageSharp.Web** — middleware for auto-resize (optional)
- For thumbnail: resize on upload, store original + thumbnail variants

## Integration Events (Potential)
- `StoreCreatedEvent` — notify Catalog that a new store is ready
- `SellerVerifiedEvent` — notify other services of seller verification status
- (These can be deferred to when Catalog/Ordering actually need them)
