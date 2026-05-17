using Scalar.Aspire;

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

// Redis — used by Cart.API, Notification.Worker (SignalR backplane)
var redis = builder.AddRedis("redis")
    .WithRedisInsight();

// RabbitMQ — message broker for MassTransit
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();
    

// ──────────────────────────────────────────────
// Elasticsearch — used by Search.API
// ──────────────────────────────────────────────
// Elastic.Clients.Elasticsearch 9.4.0 requires ES server 9.x
var elasticsearch = builder.AddElasticsearch("elasticsearch")
    .WithImage("elasticsearch")
    .WithImageTag("9.0.1");

// ──────────────────────────────────────────────
// Microservices (to be added in later phases)
// ──────────────────────────────────────────────

// Phase 1: Identity.API  -> .WithReference(identityDb).WithReference(messaging)
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb)
    .WaitFor(identityDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    // Provide JWT configuration / secrets for local development
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 2: Search.API must come up before Catalog dev seeding publishes events.
var searchApi = builder.AddProject<Projects.Search_API>("search-api")
    .WithReference(elasticsearch)
    .WaitFor(elasticsearch)
    .WithReference(messaging)
    .WaitFor(messaging);

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitFor(searchApi)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 3: Inventory.API
var inventoryApi = builder.AddProject<Projects.Inventory_API>("inventory-api")
    .WithReference(inventoryDb)
    .WaitFor(inventoryDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 3: Cart.API
var cartApi = builder.AddProject<Projects.Cart_API>("cart-api")
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
var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(orderingDb)
    .WaitFor(orderingDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 4: Payment.API
var paymentApi = builder.AddProject<Projects.Payment_API>("payment-api")
    .WithReference(paymentDb)
    .WaitFor(paymentDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 5: Notification.Worker
var notificationWorker = builder.AddProject<Projects.Notification_Worker>("notification-api")
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithExternalHttpEndpoints();

// Phase 6: Azure Blob Storage (Azurite emulator locally)
var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blobs = storage.AddBlobs("blobs");

// Phase 6: StoreManagement.API
var storeApi = builder.AddProject<Projects.StoreManagement_API>("store-api")
    .WithReference(storeDb)
    .WaitFor(storeDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 6: Media.API
var mediaApi = builder.AddProject<Projects.Media_API>("media-api")
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__Issuer", "marketplace-identity")
    .WithEnvironment("Jwt__Audience", "marketplace-api")
    .WithEnvironment("Jwt__Secret", "super-secret-key-for-dev-only-min-32-chars!!");

// Phase 1: ApiGateway    -> .WithReference(redis)
var gateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
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
    .WithReference(paymentApi)
    .WaitFor(paymentApi)
    .WithReference(notificationWorker)
    .WaitFor(notificationWorker)
    .WithReference(storeApi)
    .WaitFor(storeApi)
    .WithReference(mediaApi)
    .WaitFor(mediaApi)
    .WithReference(redis)
    .WaitFor(redis)
    .WithEnvironment("Identity__ApiBaseUrl", "http://identity-api")
    .WithExternalHttpEndpoints();

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
    .WithApiReference(gateway)
    .WithApiReference(catalogApi)
    .WithApiReference(searchApi)
    .WithApiReference(inventoryApi)
    .WithApiReference(cartApi)
    .WithApiReference(orderingApi)
    .WithApiReference(paymentApi)
    .WithApiReference(storeApi)
    .WithApiReference(mediaApi)
    .WaitFor(identityApi)
    .WaitFor(gateway)
    .WaitFor(catalogApi)
    .WaitFor(searchApi)
    .WaitFor(inventoryApi)
    .WaitFor(cartApi)
    .WaitFor(orderingApi)
    .WaitFor(paymentApi)
    .WaitFor(storeApi)
    .WaitFor(mediaApi);

// Phase 7: Angular       -> builder.AddNpmApp(...)
var frontend = builder.AddExecutable("angular", "pnpm", "../../web", "start")
    .WaitFor(scalar)
    .WithReference(gateway)
    // targetPort: 4200 tells Aspire to expect Angular on 4200.
    // port: 4201 exposes the Aspire proxy on localhost:4201 so they don't clash.
    .WithHttpEndpoint(targetPort: 4200, port: 4201, name: "http")
    .WithExternalHttpEndpoints();

builder.Build().Run();
