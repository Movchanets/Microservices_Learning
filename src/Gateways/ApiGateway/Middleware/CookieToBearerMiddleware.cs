using Microsoft.AspNetCore.Authentication;

namespace ApiGateway.Middleware;

/// <summary>
/// Reads the access token from the encrypted session cookie and injects it
/// as a Bearer Authorization header before YARP proxies the request.
/// </summary>
public sealed class CookieToBearerMiddleware(RequestDelegate next)
{
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
