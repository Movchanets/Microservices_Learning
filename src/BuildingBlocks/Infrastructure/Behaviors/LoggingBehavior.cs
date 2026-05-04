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
