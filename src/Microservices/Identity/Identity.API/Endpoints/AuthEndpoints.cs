using Identity.Application.Commands.Login;
using Identity.Application.Commands.Register;
using Identity.Application.Commands.RefreshToken;
using MediatR;

namespace Identity.API.Endpoints;

/// <summary>
/// Registers the authentication-related endpoints for the Minimal API.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps the authentication endpoints.
    /// Rationale: Groups all auth routes under a common prefix and tag. Endpoints delegate business logic
    /// completely to MediatR, keeping the API layer thin.
    /// </summary>
    /// <param name="app">The route builder instance.</param>
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/auth")
            .WithTags("Authentication");

        group.MapPost("/register", async (
            RegisterUserCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            // Rationale: Return 201 Created on success, including the location of the newly created resource.
            return result.IsSuccess
                ? Results.Created($"/api/identity/users/{result.Value!.Email}", result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("RegisterUser")
        .Produces<Identity.Application.DTOs.AuthResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/login", async (
            LoginUserCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            // Rationale: Return 401 Unauthorized for bad credentials rather than 400 Bad Request
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Unauthorized();
        })
        .WithName("LoginUser")
        .Produces<Identity.Application.DTOs.AuthResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", async (
            RefreshTokenCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Unauthorized();
        })
        .WithName("RefreshToken")
        .Produces<Identity.Application.DTOs.AuthResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
