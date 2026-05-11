using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// Infrastructure Resources
// ──────────────────────────────────────────────

// PostgreSQL server + per-service databases
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var identityDb = postgres.AddDatabase("identity-db");
var catalogDb = postgres.AddDatabase("catalog-db");
var orderingDb = postgres.AddDatabase("ordering-db");
var inventoryDb = postgres.AddDatabase("inventory-db");
var paymentDb = postgres.AddDatabase("payment-db");
var storeDb = postgres.AddDatabase("store-db");

// Redis — used by Cart.API, Notification.Worker (SignalR backplane)
var redis = builder.AddRedis("redis")
    .WithRedisInsight();

// RabbitMQ — message broker for MassTransit
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// ──────────────────────────────────────────────
// Microservices (to be added in later phases)
// ──────────────────────────────────────────────

// Phase 1: Identity.API  → .WithReference(identityDb).WithReference(messaging)
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb)
    .WaitFor(identityDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    // Provide JWT configuration / secrets for local development
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 1: ApiGateway    → .WithReference(redis)
var gateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithReference(identityApi)
    .WaitFor(identityApi)
    .WithReference(redis)
    .WaitFor(redis)
    .WithEnvironment("Identity__ApiBaseUrl", "http://identity-api")
    .WithExternalHttpEndpoints();
// Phase 2: Catalog.API   → .WithReference(catalogDb).WithReference(messaging)
// Phase 2: Search.API    → .WithReference(messaging)
// Phase 3: Inventory.API → .WithReference(inventoryDb).WithReference(messaging)
// Phase 3: Cart.API      → .WithReference(redis).WithReference(messaging)
// Phase 4: Ordering.API  → .WithReference(orderingDb).WithReference(messaging)
// Phase 4: Payment.API   → .WithReference(paymentDb).WithReference(messaging)
// Phase 5: Notification  → .WithReference(redis).WithReference(messaging)
// Phase 6: StoreMgmt.API → .WithReference(storeDb).WithReference(messaging)
// Phase 6: Media.API     → blob storage reference

// ──────────────────────────────────────────────
// Scalar API Reference — unified docs for all services
// ──────────────────────────────────────────────
var scalar = builder.AddScalarApiReference(options =>
{
    options.WithTheme(ScalarTheme.Purple);
});

// Register implemented services (add new services here as they come online)
scalar
    .WithApiReference(identityApi)
    .WithApiReference(gateway);
// Phase 2: .WithApiReference(catalogApi)
// Phase 2: .WithApiReference(searchApi)
// Phase 3: .WithApiReference(inventoryApi)
// Phase 3: .WithApiReference(cartApi)
// Phase 4: .WithApiReference(orderingApi)
// Phase 4: .WithApiReference(paymentApi)
// Phase 5: .WithApiReference(notificationWorker)
// Phase 6: .WithApiReference(storeApi)
// Phase 6: .WithApiReference(mediaApi)

// Phase 7: Angular       → builder.AddNpmApp(...)
var frontend = builder.AddExecutable("angular", "pnpm", "../../web", "start")
    .WithReference(gateway)
    // targetPort: 4200 tells Aspire to expect Angular on 4200.
    // port: 4201 exposes the Aspire proxy on localhost:4201 so they don't clash.
    .WithHttpEndpoint(targetPort: 4200, port: 4201, name: "http")
    .WithExternalHttpEndpoints();
builder.Build().Run();
