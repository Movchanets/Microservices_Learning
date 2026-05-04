using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Marketplace.ServiceDefaults;

public static class Extensions
{
    private const string HealthEndpointPath  = "/health";
    private const string AlivenessEndpointPath = "/alive";

    /// <summary>
    /// Adds common .NET Aspire services: telemetry, health checks, service discovery, resilience.
    /// Every microservice MUST call this in Program.cs / AppHost setup.
    /// </summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Add resilience (retries, circuit breaker, timeout)
            http.AddStandardResilienceHandler();

            // Enable service discovery for all HttpClient instances
            http.AddServiceDiscovery();
        });

        // Uncomment to restrict allowed schemes for service discovery:
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry with tracing (with health-check path exclusion) and metrics exporters.
    /// </summary>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(opt =>
                        // Exclude health probe requests from traces (reduces noise)
                        opt.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !ctx.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment to enable Azure Monitor / Application Insights:
        // if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        // {
        //     builder.Services.AddOpenTelemetry().UseAzureMonitor();
        // }

        return builder;
    }

    /// <summary>
    /// Adds default health check endpoints (liveness + readiness).
    /// </summary>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps /health (readiness) and /alive (liveness) endpoints.
    /// Call this in the WebApplication pipeline after UseAuthentication/UseAuthorization.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // In non-development environments, health checks require additional security consideration.
        // See: https://aka.ms/dotnet/aspire/healthchecks
        if (app.Environment.IsDevelopment())
        {
            // Readiness probe — all checks must pass
            app.MapHealthChecks(HealthEndpointPath);

            // Liveness probe — only "live"-tagged checks
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
