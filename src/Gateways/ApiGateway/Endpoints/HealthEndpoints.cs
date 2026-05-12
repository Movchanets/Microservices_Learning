namespace ApiGateway.Endpoints;

/// <summary>
/// Aggregated health-check endpoint that probes every microservice's /health endpoint.
/// Each service exposes /health via Marketplace.ServiceDefaults.MapDefaultEndpoints().
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Known microservice names → must match named HttpClients registered in Program.cs.
    /// When a new service is added to the AppHost, add its name here and register its HttpClient.
    /// </summary>
    private static readonly string[] ServiceNames =
    [
        "identity-api",
        //uncomment when implemented
        //"catalog-api",
        //"ordering-api",
        //"inventory-api",
        //"cart-api",
        //"search-api",
        //"store-api",
        //"media-api",
        //"payment-api",
        //"notification-worker"
    ];

    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Aggregated health: probes all registered microservices in parallel
        app.MapGet("/bff/health", async (IHttpClientFactory httpClientFactory, CancellationToken ct) =>
        {
            var results = new Dictionary<string, ServiceHealthStatus>();
            var tasks = ServiceNames.Select(async service =>
            {
                var status = await ProbeServiceAsync(httpClientFactory, service, ct);
                return (service, status);
            });

            foreach (var (service, status) in await Task.WhenAll(tasks))
            {
                results[service] = status;
            }

            var overallHealthy = results.Values.All(s => s.Status == "Healthy");

            return overallHealthy
                ? Results.Ok(new AggregatedHealthResponse("Healthy", results))
                : Results.Json(
                    new AggregatedHealthResponse("Degraded", results),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
        })
        .AllowAnonymous()
        .WithTags("Health")
        .WithSummary("Aggregated service health")
        .WithDescription("Probes every registered microservice's /health endpoint in parallel. Returns 200 Healthy when all pass, 503 Degraded if any fail.")
        .Produces<AggregatedHealthResponse>(StatusCodes.Status200OK)
        .Produces<AggregatedHealthResponse>(StatusCodes.Status503ServiceUnavailable)
        .WithOpenApi();

        // Per-service health: /bff/health/{serviceName}
        app.MapGet("/bff/health/{serviceName}", async (
            string serviceName,
            IHttpClientFactory httpClientFactory,
            CancellationToken ct) =>
        {
            if (!ServiceNames.Contains(serviceName, StringComparer.OrdinalIgnoreCase))
            {
                return Results.NotFound(new { error = $"Unknown service: {serviceName}" });
            }

            var status = await ProbeServiceAsync(httpClientFactory, serviceName, ct);

            return status.Status == "Healthy"
                ? Results.Ok(status)
                : Results.Json(status, statusCode: StatusCodes.Status503ServiceUnavailable);
        })
        .AllowAnonymous()
        .WithTags("Health")
        .WithSummary("Single service health")
        .WithDescription("Probes the /health endpoint of the named microservice. Returns 200 Healthy or 503 Unhealthy. 404 if the service name is unknown.")
        .Produces<ServiceHealthStatus>(StatusCodes.Status200OK)
        .Produces<ServiceHealthStatus>(StatusCodes.Status503ServiceUnavailable)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi();
    }

    private static async Task<ServiceHealthStatus> ProbeServiceAsync(
        IHttpClientFactory httpClientFactory,
        string serviceName,
        CancellationToken ct)
    {
        try
        {
            var http = httpClientFactory.CreateClient(serviceName);
            var response = await http.GetAsync("/health", ct);

            return response.IsSuccessStatusCode
                ? new ServiceHealthStatus("Healthy", (int)response.StatusCode, null)
                : new ServiceHealthStatus("Unhealthy", (int)response.StatusCode, response.ReasonPhrase);
        }
        catch (HttpRequestException ex)
        {
            return new ServiceHealthStatus("Unhealthy", null, ex.Message);
        }
        catch (TaskCanceledException)
        {
            return new ServiceHealthStatus("Unhealthy", null, "Request timed out");
        }
    }

    private sealed record ServiceHealthStatus(string Status, int? StatusCode, string? Error);
    private sealed record AggregatedHealthResponse(string Status, Dictionary<string, ServiceHealthStatus> Services);
}
