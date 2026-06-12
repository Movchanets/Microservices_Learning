using BuildingBlocks.Infrastructure.Middleware;
using Elastic.Clients.Elasticsearch;
using Marketplace.ServiceDefaults;
using MassTransit;
using Search.API.Consumers;
using Search.API.Endpoints;
using Search.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire ServiceDefaults ──────────────────────────────
builder.AddServiceDefaults();

// ── Elasticsearch ───────────────────────────────────────
var elasticUri = builder.Configuration.GetConnectionString("elasticsearch")
    ?? "http://localhost:9200";

builder.Services.AddSingleton(_ =>
{
    var settings = new ElasticsearchClientSettings(new Uri(elasticUri))
        .DefaultIndex("marketplace-products")
        .EnableDebugMode()
        .RequestTimeout(TimeSpan.FromSeconds(30));
    return new ElasticsearchClient(settings);
});

builder.Services.AddHostedService<ElasticsearchInitializer>();
builder.Services.AddSingleton<ISearchService, ElasticsearchService>();

// ── MassTransit (consumers) ─────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<ProductCreatedConsumer>();
    x.AddConsumer<ProductUpdatedConsumer>();
    x.AddConsumer<SkuCreatedConsumer>();
    x.AddConsumer<SkuDeletedConsumer>();
    x.AddConsumer<SkuPriceChangedConsumer>();
    x.AddConsumer<ProductDeletedConsumer>();
    x.AddConsumer<MediaGalleryUpdatedConsumer>();
    x.AddConsumer<MediaUploadedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("messaging"));
        cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("search", false));
    });
});

// ── OpenAPI ─────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware pipeline ─────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapDefaultEndpoints(); // health checks
app.MapOpenApi();

// ── Endpoints ───────────────────────────────────────────
app.MapSearchEndpoints();

app.Run();
