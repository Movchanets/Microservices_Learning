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
	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await next(context);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
			await HandleExceptionAsync(context, ex);
		}
	}

	private async Task HandleExceptionAsync(HttpContext context, Exception exception)
	{
		var (statusCode, title) = exception switch
		{
			ArgumentException => (HttpStatusCode.BadRequest, "Bad Request"),
			KeyNotFoundException => (HttpStatusCode.NotFound, "Not Found"),
			UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
			InvalidOperationException => (HttpStatusCode.Conflict, "Conflict"),
			_ => (HttpStatusCode.InternalServerError, "Internal Server Error")
		};

		var problemDetails = new ProblemDetails
		{
			Status = (int)statusCode,
			Title = title,
			Detail = env.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
			Instance = context.Request.Path
		};

		context.Response.StatusCode = (int)statusCode;
		context.Response.ContentType = "application/problem+json";

		await context.Response.WriteAsJsonAsync(problemDetails);
	}
}
