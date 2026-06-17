using System.Text.Json;
using Marketplace.ServiceDefaults;
using Seeder.App;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient("ApiGateway", client =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(baseUrl);
});

// Direct client for media uploads (bypasses YARP gateway to avoid multipart form forwarding issues)
builder.Services.AddHttpClient("MediaApi", client =>
{
    var mediaUrl = builder.Configuration["services:media-api:http:0"] ?? "http://localhost:5361";
    client.BaseAddress = new Uri(mediaUrl);
});

// Named client for downloading images from external URLs (Rozetka, etc.)
builder.Services.AddHttpClient("download");

// Configure default JSON options for extension methods
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
