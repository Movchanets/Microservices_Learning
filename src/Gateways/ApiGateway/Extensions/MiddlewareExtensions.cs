using ApiGateway.Middleware;

namespace ApiGateway.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCookieToBearer(this IApplicationBuilder app) =>
        app.UseMiddleware<CookieToBearerMiddleware>();

    public static IApplicationBuilder UseCsrfValidation(this IApplicationBuilder app) =>
        app.UseMiddleware<CsrfValidationMiddleware>();
}
