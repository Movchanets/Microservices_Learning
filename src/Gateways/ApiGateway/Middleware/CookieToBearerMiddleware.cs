using Microsoft.AspNetCore.Authentication;

namespace ApiGateway.Middleware;

/// <summary>
/// Reads the access token from the encrypted session cookie and injects it
/// as a Bearer Authorization header before YARP proxies the request.
/// </summary>
public sealed class CookieToBearerMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Processes a request to transform the cookie-based session into a Bearer token header.
    /// Rationale: Implements the Backend-For-Frontend (BFF) pattern. The Angular SPA securely uses HttpOnly cookies
    /// to authenticate. This middleware extracts the stored JWT and appends it to the Authorization header
    /// so downstream microservices (which expect JWT Bearer auth) can process the request normally.
    /// </summary>
    /// <param name="context">The HttpContext for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var token = await context.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                context.Request.Headers.Authorization = $"Bearer {token}";
            }
        }

        await next(context);
    }
}
