using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

const string HttpLaunchProfile = "http";
const string JwtIssuer = "marketplace-identity";
const string JwtAudience = "marketplace-api";
const string JwtSecret = "super-secret-key-for-dev-only-min-32-chars!!";
const int GatewayHttpPort = 5390;
const string GatewayHttpUrl = "http://localhost:5390";

// IMPORTANT: using the "http" launch profile for every project prevents DCP
// from registering HTTPS endpoints, which avoids the Windows container tunnel
// crash seen in local development.
IResourceBuilder<ProjectResource> AddHttpProject<TProject>(string resourceName)
    where TProject : IProjectMetadata, new()
    => builder.AddProject<TProject>(resourceName, HttpLaunchProfile);

IResourceBuilder<ProjectResource> WithMarketplaceJwt(IResourceBuilder<ProjectResource> project) =>
    project
        .WithEnvironment("Jwt__Issuer", JwtIssuer)
        .WithEnvironment("Jwt__Audience", JwtAudience)
        .WithEnvironment("Jwt__Secret", JwtSecret);

IResourceBuilder<ProjectResource> WithRequiredProject(
    IResourceBuilder<ProjectResource> project,
    IResourceBuilder<ProjectResource> dependency) =>
    project.WithReference(dependency).WaitFor(dependency);

// ──────────────────────────────────────────────
// Infrastructure Resources
// ──────────────────────────────────────────────

var isTesting = builder.Environment.EnvironmentName == "Testing";

var postgres = builder.AddPostgres("postgres")
    .WithHostPort(55555);
if (!isTesting) postgres.WithPgAdmin();

var identityDb = postgres.AddDatabase("identity-db");
var catalogDb = postgres.AddDatabase("catalog-db");
var orderingDb = postgres.AddDatabase("ordering-db");
var inventoryDb = postgres.AddDatabase("inventory-db");
var paymentDb = postgres.AddDatabase("payment-db");
var storeDb = postgres.AddDatabase("store-db");
var cartDb = postgres.AddDatabase("cart-db");
var mediaDb = postgres.AddDatabase("media-db");

var redis = builder.AddRedis("redis");
if (!isTesting) redis.WithRedisInsight();

var messaging = builder.AddRabbitMQ("messaging");
if (!isTesting) messaging.WithManagementPlugin();

var elasticsearch = builder.AddElasticsearch("elasticsearch")
    .WithImage("elasticsearch")
    .WithImageTag("8.17.0")
    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m");

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blobs = storage.AddBlobs("blobs");

IResourceBuilder<ProjectResource> WithMessaging(IResourceBuilder<ProjectResource> project) =>
    project.WithReference(messaging).WaitFor(messaging);

IResourceBuilder<ProjectResource> WithSecuredMessaging(IResourceBuilder<ProjectResource> project) =>
    WithMarketplaceJwt(WithMessaging(project));

// ──────────────────────────────────────────────
// Microservices
// ──────────────────────────────────────────────

var identityApi = WithSecuredMessaging(
    AddHttpProject<Projects.Identity_API>("identity-api")
        .WithReference(identityDb)
        .WaitFor(identityDb));

var searchApi = WithMessaging(
    AddHttpProject<Projects.Search_API>("search-api")
        .WithReference(elasticsearch)
        .WaitFor(elasticsearch));

var catalogApi = WithSecuredMessaging(
    AddHttpProject<Projects.Catalog_API>("catalog-api")
        .WithReference(catalogDb)
        .WaitFor(catalogDb));

var inventoryApi = WithSecuredMessaging(
    AddHttpProject<Projects.Inventory_API>("inventory-api")
        .WithReference(inventoryDb)
        .WaitFor(inventoryDb));

var cartApi = WithSecuredMessaging(
    AddHttpProject<Projects.Cart_API>("cart-api")
        .WithReference(cartDb)
        .WaitFor(cartDb)
        .WithReference(redis)
        .WaitFor(redis));

var orderingApi = WithSecuredMessaging(
    AddHttpProject<Projects.Ordering_API>("ordering-api")
        .WithReference(orderingDb)
        .WaitFor(orderingDb));

// Catalog depends on Ordering for verified-purchase checks.
catalogApi.WithReference(orderingApi).WaitFor(orderingApi);

var paymentApi = WithSecuredMessaging(
    AddHttpProject<Projects.Payment_API>("payment-api")
        .WithReference(paymentDb)
        .WaitFor(paymentDb));

var notificationWorker = WithMessaging(
    AddHttpProject<Projects.Notification_Worker>("notification-api")
        .WithReference(redis)
        .WaitFor(redis))
    .WithExternalHttpEndpoints();

var storeApi = WithSecuredMessaging(
    AddHttpProject<Projects.StoreManagement_API>("store-api")
        .WithReference(storeDb)
        .WaitFor(storeDb));

var mediaApi = WithSecuredMessaging(
    AddHttpProject<Projects.Media_API>("media-api")
        .WithReference(mediaDb)
        .WaitFor(mediaDb)
        .WithReference(blobs)
        .WaitFor(blobs));

// ──────────────────────────────────────────────
// Gateway, Frontend, and Tools
// ──────────────────────────────────────────────

var gateway = WithMarketplaceJwt(AddHttpProject<Projects.ApiGateway>("api-gateway"));

// Hard dependencies block gateway startup because the BFF cannot function
// correctly without their routes and service discovery entries.
foreach (var dependency in new[]
{
    identityApi,
    catalogApi,
    searchApi,
    inventoryApi,
    cartApi,
    orderingApi,
})
{
    gateway = WithRequiredProject(gateway, dependency);
}

// Soft dependencies are exposed for service discovery but do not block startup.
foreach (var dependency in new[]
{
    paymentApi,
    notificationWorker,
    storeApi,
    mediaApi,
})
{
    gateway = gateway.WithReference(dependency);
}

gateway = gateway
    .WithReference(redis)
    .WaitFor(redis)
    .WithEnvironment("Identity__ApiBaseUrl", "http://identity-api")
    // Non-proxied: gateway listens directly on port 5390, bypassing DCP port
    // forwarding which is broken on Windows. ASPNETCORE_URLS is set explicitly
    // because --no-launch-profile ignores launchSettings.json.
    // Do not call WithExternalHttpEndpoints() here; that makes DCP claim the
    // same port and can trigger WSAEACCES 10013 on Windows.
    .WithEnvironment("ASPNETCORE_URLS", GatewayHttpUrl)
    .WithEndpoint("http", endpoint =>
    {
        endpoint.IsProxied = false;
        endpoint.Port = GatewayHttpPort;
        endpoint.TargetPort = GatewayHttpPort;
    });

var frontend = builder.AddExecutable("angular", "pnpm", "../../web", "start")
    .WithReference(gateway)
    .WithHttpEndpoint(targetPort: 4200, port: 4201, name: "http")
    .WithExternalHttpEndpoints();

var seederApp = AddHttpProject<Projects.Seeder_App>("seeder-app")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithEnvironment("ApiBaseUrl", gateway.GetEndpoint("http"));

builder.Build().Run();
