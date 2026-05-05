var builder = DistributedApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// Infrastructure Resources
// ──────────────────────────────────────────────

// PostgreSQL server + per-service databases
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var identityDb  = postgres.AddDatabase("identity-db");
var catalogDb   = postgres.AddDatabase("catalog-db");
var orderingDb  = postgres.AddDatabase("ordering-db");
var inventoryDb = postgres.AddDatabase("inventory-db");
var paymentDb   = postgres.AddDatabase("payment-db");
var storeDb     = postgres.AddDatabase("store-db");

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
    .WithReference(messaging);

// Phase 1: ApiGateway    → .WithReference(redis)
var gateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithReference(identityApi)
    .WithReference(redis)
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
// Phase 7: Angular       → builder.AddNpmApp(...)

builder.Build().Run();
