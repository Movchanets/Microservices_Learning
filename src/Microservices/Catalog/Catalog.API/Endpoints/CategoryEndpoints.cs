using Catalog.Application.Commands.CreateCategory;
using Catalog.Application.Commands.DeleteCategory;
using Catalog.Application.Commands.UpdateCategory;
using Catalog.Application.DTOs;
using Catalog.Application.Queries;
using MediatR;

namespace Catalog.API.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog/categories")
            .WithTags("Categories")
            .WithOpenApi();

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

        // Authorized: create category
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

        // Authorized: update category
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCategoryCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = command with { Id = id };
            var result = await sender.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("UpdateCategory")
        .RequireAuthorization()
        .Produces<CategoryDto>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Authorized: delete category (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteCategoryCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("DeleteCategory")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
