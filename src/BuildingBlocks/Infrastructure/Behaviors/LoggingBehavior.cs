using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs request start/end with elapsed time.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Intercepts the MediatR request to log the start, end, and execution time of the handler.
    /// Rationale: Implements a cross-cutting concern (logging) using the Decorator/Pipeline pattern.
    /// This keeps command and query handlers clean and focused on business logic while
    /// providing consistent observability and performance monitoring (slow request warnings) out of the box.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">The delegate to call the next behavior or the handler itself.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response from the next behavior or handler.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("[START] {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > 500)
        {
            logger.LogWarning("[SLOW] {RequestName} took {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);
        }

        logger.LogInformation("[END] {RequestName} ({ElapsedMs}ms)",
            requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
