using Catalog.Application.Commands.CreateCategory;
using Catalog.Application.DTOs;
using Catalog.Application.Queries;
using MediatR;

namespace Catalog.API.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog/categories")
            .WithTags("Categories");

        // Public: list categories
        group.MapGet("/", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var categories = await sender.Send(new ListCategoriesQuery(), ct);
            return Results.Ok(categories);
        })
        .WithName("ListCategories")
        .Produces<List<CategoryDto>>();

        // Authorized: create category (admin only)
        group.MapPost("/", async (
            CreateCategoryCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/catalog/categories/{result.Value!.Id}", result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("CreateCategory")
        .RequireAuthorization()
        .Produces<CategoryDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
