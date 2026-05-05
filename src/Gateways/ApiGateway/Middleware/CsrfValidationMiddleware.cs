namespace ApiGateway.Middleware;

/// <summary>
/// Validates CSRF token on mutating requests (POST, PUT, DELETE, PATCH).
/// Angular sends X-XSRF-TOKEN header from a non-HttpOnly cookie.
/// </summary>
public sealed class CsrfValidationMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> MutatingMethods =
        ["POST", "PUT", "DELETE", "PATCH"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (MutatingMethods.Contains(context.Request.Method) &&
            context.User.Identity?.IsAuthenticated == true)
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
