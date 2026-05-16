using System.Security.Claims;
using Identity.Application.Commands.DeactivateUser;
using Identity.Application.Commands.UpdateUserRole;
using Identity.Application.DTOs;
using Identity.Application.Queries;
using MediatR;

namespace Identity.API.Endpoints;

/// <summary>
/// Registers the user management endpoints for the Minimal API.
/// </summary>
public static class UserEndpoints
{
    /// <summary>
    /// Maps the user endpoints.
    /// Rationale: Groups all user management endpoints and requires authorization at the group level.
    /// </summary>
    /// <param name="app">The route builder instance.</param>
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/users")
            .WithTags("Users")
            .RequireAuthorization();

        // List all users (admin only)
        group.MapGet("/", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var users = await sender.Send(new ListUsersQuery(), ct);
            return Results.Ok(users);
        })
        .WithName("ListUsers")
        .RequireAuthorization("Admin")
        .Produces<List<UserDto>>();

        // Get user by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var user = await sender.Send(new GetUserByIdQuery(id), ct);
            return user is not null
                ? Results.Ok(user)
                : Results.NotFound();
        })
        .WithName("GetUserById")
        .Produces<UserDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Update user role (admin only)
        group.MapPut("/{id:guid}/role", async (
            Guid id,
            UpdateUserRoleCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = command with { UserId = id };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("UpdateUserRole")
        .RequireAuthorization("Admin")
        .Produces<UserDto>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Deactivate user (admin only)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeactivateUserCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("DeactivateUser")
        .RequireAuthorization("Admin")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
