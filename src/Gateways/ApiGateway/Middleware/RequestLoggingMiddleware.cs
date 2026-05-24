using System.Diagnostics;

namespace ApiGateway.Middleware;

/// <summary>
/// Logs HTTP request method, path, status code, and elapsed time.
/// </summary>
public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("HTTP {Method} {Path} started",
            context.Request.Method, context.Request.Path);

        await next(context);

        sw.Stop();
        logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
            context.Request.Method, context.Request.Path,
            context.Response.StatusCode, sw.ElapsedMilliseconds);
    }
}
