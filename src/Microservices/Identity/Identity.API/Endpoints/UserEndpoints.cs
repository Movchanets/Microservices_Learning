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
    }
}
