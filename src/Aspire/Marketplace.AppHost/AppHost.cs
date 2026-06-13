
var builder = DistributedApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// Infrastructure Resources
// ──────────────────────────────────────────────

// PostgreSQL server + per-service databases
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithHostPort(55555);

var identityDb = postgres.AddDatabase("identity-db");
var catalogDb = postgres.AddDatabase("catalog-db");
var orderingDb = postgres.AddDatabase("ordering-db");
var inventoryDb = postgres.AddDatabase("inventory-db");
var paymentDb = postgres.AddDatabase("payment-db");
var storeDb = postgres.AddDatabase("store-db");
var cartDb = postgres.AddDatabase("cart-db");
var mediaDb = postgres.AddDatabase("media-db");

// Redis — used by Cart.API, Notification.Worker (SignalR backplane)
var redis = builder.AddRedis("redis")
    .WithRedisInsight();

// RabbitMQ — message broker for MassTransit
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();


// ──────────────────────────────────────────────
// Elasticsearch — used by Search.API
// ──────────────────────────────────────────────
var elasticsearch = builder.AddElasticsearch("elasticsearch")
    .WithImage("elasticsearch")
    .WithImageTag("8.17.0")
    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m");

// ──────────────────────────────────────────────
// Microservices
// ──────────────────────────────────────────────

// IMPORTANT: "http" launch profile on every AddProject prevents DCP from
// registering HTTPS endpoints, which avoids the Windows container tunnel crash.

// Phase 1: Identity.API
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api", "http")
    .WithReference(identityDb)
    .WaitFor(identityDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 2: Search.API
var searchApi = builder.AddProject<Projects.Search_API>("search-api", "http")
    .WithReference(elasticsearch)
    .WaitFor(elasticsearch)
    .WithReference(messaging)
    .WaitFor(messaging);

// Phase 2: Catalog.API
var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api", "http")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 3: Inventory.API
var inventoryApi = builder.AddProject<Projects.Inventory_API>("inventory-api", "http")
    .WithReference(inventoryDb)
    .WaitFor(inventoryDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 3: Cart.API
var cartApi = builder.AddProject<Projects.Cart_API>("cart-api", "http")
    .WithReference(cartDb)
    .WaitFor(cartDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 4: Ordering.API
var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api", "http")
    .WithReference(orderingDb)
    .WaitFor(orderingDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Catalog depends on Ordering for verified-purchase checks
catalogApi.WithReference(orderingApi).WaitFor(orderingApi);

// Phase 4: Payment.API
var paymentApi = builder.AddProject<Projects.Payment_API>("payment-api", "http")
    .WithReference(paymentDb)
    .WaitFor(paymentDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 5: Notification.Worker
var notificationWorker = builder.AddProject<Projects.Notification_Worker>("notification-api", "http")
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithExternalHttpEndpoints();

// Phase 6: Azure Blob Storage (Azurite emulator locally)
var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blobs = storage.AddBlobs("blobs");

// Phase 6: StoreManagement.API
var storeApi = builder.AddProject<Projects.StoreManagement_API>("store-api", "http")
    .WithReference(storeDb)
    .WaitFor(storeDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 6: Media.API
var mediaApi = builder.AddProject<Projects.Media_API>("media-api", "http")
    .WithReference(mediaDb)
    .WaitFor(mediaDb)
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 1: ApiGateway
// Gateway WaitFor only hard prerequisites (identity, catalog, search).
// Soft dependencies (payment, notification, store, media) use WithReference
// for service discovery but don't block gateway startup.
var gateway = builder.AddProject<Projects.ApiGateway>("api-gateway", "http")
    // Hard dependencies — gateway can't function without these
    .WithReference(identityApi)
    .WaitFor(identityApi)
    .WithReference(catalogApi)
    .WaitFor(catalogApi)
    .WithReference(searchApi)
    .WaitFor(searchApi)
    .WithReference(inventoryApi)
    .WaitFor(inventoryApi)
    .WithReference(cartApi)
    .WaitFor(cartApi)
    .WithReference(orderingApi)
    .WaitFor(orderingApi)
    // Soft dependencies — service discovery only, don't block startup
    .WithReference(paymentApi)
    .WithReference(notificationWorker)
    .WithReference(storeApi)
    .WithReference(mediaApi)
    .WithReference(redis)
    .WaitFor(redis)
    .WithEnvironment("Identity__ApiBaseUrl", "http://identity-api")
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!")
    .WithExternalHttpEndpoints();

// Phase 7: Angular frontend
var frontend = builder.AddExecutable("angular", "pnpm", "../../web", "start")
    .WithReference(gateway)
    .WithHttpEndpoint(targetPort: 4200, port: 4201, name: "http")
    .WithExternalHttpEndpoints();

// Seeder — waits for gateway so it can reach all services through BFF
var seederApp = builder.AddProject<Projects.Seeder_App>("seeder-app", "http")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithEnvironment("ApiBaseUrl", gateway.GetEndpoint("http"));

builder.Build().Run();
