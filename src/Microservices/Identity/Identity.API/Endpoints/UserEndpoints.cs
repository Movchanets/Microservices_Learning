using Identity.Application.Queries;
using MediatR;

namespace Identity.API.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/users")
            .WithTags("Users")
            .WithOpenApi()
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
