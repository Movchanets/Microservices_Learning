using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Middleware;

/// <summary>
/// Catches unhandled exceptions and returns RFC 7807 ProblemDetails.
/// Register in every microservice pipeline: app.UseMiddleware&lt;GlobalExceptionMiddleware&gt;();
/// </summary>
public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment env)
{
    /// <summary>
    /// Processes an incoming HTTP request, catching any unhandled exceptions
    /// and transforming them into a standard ProblemDetails response.
    /// Rationale: Centralizing error handling here ensures consistent API error shapes
    /// (RFC 7807 ProblemDetails) across all microservices without duplicating try-catch blocks in endpoints.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning("Request cancelled: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Formats the caught exception into an HTTP response using ProblemDetails.
    /// Rationale: Maps common exception types to appropriate HTTP status codes,
    /// abstracting technical exceptions into standard HTTP semantics for API consumers.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="exception">The caught exception.</param>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            OperationCanceledException => (HttpStatusCode.ServiceUnavailable, "Request Cancelled"),
            ArgumentException => (HttpStatusCode.BadRequest, "Bad Request"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Not Found"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            InvalidOperationException => (HttpStatusCode.Conflict, "Conflict"),
            // EF Core exceptions — detected by type name to avoid adding EF Core dependency to BuildingBlocks
            _ when exception.GetType().Name is "DbUpdateConcurrencyException" or "DbUpdateException"
                => (HttpStatusCode.Conflict, "Data Conflict"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        // For data conflicts, provide a user-friendly message in production too
        var detail = exception.GetType().Name switch
        {
            "DbUpdateConcurrencyException" => "The data was modified by another request. Please retry.",
            "DbUpdateException" => env.IsDevelopment() ? exception.Message : "A data conflict occurred. Please retry.",
            _ => env.IsDevelopment() ? exception.Message : "An unexpected error occurred."
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
