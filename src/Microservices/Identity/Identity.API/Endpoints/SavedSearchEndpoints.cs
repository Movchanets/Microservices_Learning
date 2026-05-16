using System.Security.Claims;
using BuildingBlocks.Infrastructure.Models;
using Identity.Application.Commands.CreateSavedSearch;
using Identity.Application.Commands.DeleteSavedSearch;
using Identity.Application.DTOs;
using Identity.Application.Queries.GetSavedSearches;
using MediatR;

namespace Identity.API.Endpoints;

public static class SavedSearchEndpoints
{
    public static void MapSavedSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/saved-searches")
            .WithTags("Saved Searches")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var result = await sender.Send(new GetSavedSearchesQuery(Guid.Parse(userId)), ct);
            return Results.Ok(result);
        })
        .WithName("GetSavedSearches")
        .Produces<List<SavedSearchDto>>();

        group.MapPost("/", async (
            CreateSavedSearchCommand command,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var cmd = command with { UserId = Guid.Parse(userId) };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Created($"/api/identity/saved-searches/{result.Value?.Id}", result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("CreateSavedSearch")
        .Produces<SavedSearchDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var result = await sender.Send(new DeleteSavedSearchCommand(id, Guid.Parse(userId)), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("DeleteSavedSearch")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
