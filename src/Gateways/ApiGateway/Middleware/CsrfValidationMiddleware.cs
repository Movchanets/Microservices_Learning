namespace ApiGateway.Middleware;

/// <summary>
/// Validates CSRF token on mutating requests (POST, PUT, DELETE, PATCH).
/// Angular sends X-XSRF-TOKEN header from a non-HttpOnly cookie.
/// </summary>
public sealed class CsrfValidationMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> MutatingMethods =
        ["POST", "PUT", "DELETE", "PATCH"];

    /// <summary>
    /// Processes the request to ensure CSRF tokens match on state-changing operations.
    /// Rationale: Because the BFF uses cookie-based authentication, it is vulnerable to Cross-Site Request Forgery.
    /// This middleware enforces the Double Submit Cookie pattern. Angular reads the XSRF-TOKEN cookie
    /// (which is purposefully NOT HttpOnly) and sends it back in the X-XSRF-TOKEN header.
    /// If they match, the request was legitimately initiated by our SPA.
    /// </summary>
    /// <param name="context">The HttpContext for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Bearer token requests are CSRF-immune — the token is set explicitly
        // by the caller, not auto-attached by the browser like cookies.
        var hasBearerToken = context.Request.Headers.Authorization
            .FirstOrDefault()?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true;

        if (MutatingMethods.Contains(context.Request.Method) &&
            context.User.Identity?.IsAuthenticated == true &&
            !hasBearerToken &&
            !context.Request.Path.StartsWithSegments("/hubs"))
        {
            var cookieToken = context.Request.Cookies["XSRF-TOKEN"];
            var headerToken = context.Request.Headers["X-XSRF-TOKEN"].FirstOrDefault();

            if (string.IsNullOrEmpty(cookieToken) ||
                string.IsNullOrEmpty(headerToken) ||
                cookieToken != headerToken)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "CSRF validation failed" });
                return;
            }
        }

        await next(context);
    }
}
