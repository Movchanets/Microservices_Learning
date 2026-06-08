using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Application.Commands.CreateCategory;
using Catalog.Application.Commands.DeleteCategory;
using Catalog.Application.Commands.UpdateCategory;
using Catalog.Application.DTOs;
using Catalog.Application.Queries;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using MediatR;

namespace Catalog.API.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog/categories")
            .WithTags("Categories")
            .WithOpenApi();

        // Public: category tree
        group.MapGet("/tree", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCategoryTreeQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetCategoryTree")
        .Produces<List<CategoryTreeDto>>();

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

        group.MapCategoryAttributeEndpoints();
    }

    private static void MapCategoryAttributeEndpoints(this RouteGroupBuilder group)
    {
        // Authorized: add attribute definition to category
        group.MapPost("/{id:guid}/attributes", async (
            Guid id,
            AddAttributeDefinitionRequest request,
            ICategoryRepository categoryRepo,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var category = await categoryRepo.GetWithAttributeDefinitionsAsync(id, ct);
            if (category is null) return Results.NotFound(new { error = "Category not found" });

            try
            {
                var attr = category.AddAttributeDefinition(
                    request.Key,
                    request.DisplayName,
                    (AttributeTarget)request.Target,
                    (AttributeType)request.ValueType,
                    request.IsFilterable,
                    request.IsRequired,
                    request.SortOrder,
                    request.AllowedValues);

                // EF Core detects the new entity as Added because Id is Guid.Empty
                // (Guid v7 is generated on insert by GuidV7ValueGenerator).
                categoryRepo.Update(category);
                await unitOfWork.SaveChangesAsync(ct);

                var dto = new AttributeDefinitionDto(
                    attr.Id,
                    attr.Key,
                    attr.DisplayName,
                    attr.Target.ToString(),
                    attr.ValueType.ToString(),
                    attr.IsFilterable,
                    attr.IsRequired,
                    attr.SortOrder,
                    attr.AllowedValues);

                return Results.Created($"/api/catalog/categories/{id}/attributes/{attr.Id}", dto);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("AddAttributeDefinition")
        .RequireAuthorization()
        .Produces<AttributeDefinitionDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Public: get attribute definitions for a category
        group.MapGet("/{id:guid}/attributes", async (
            Guid id,
            bool? includeInherited,
            ICategoryRepository categoryRepo,
            CancellationToken ct) =>
        {
            var category = await categoryRepo.GetWithAttributeDefinitionsAsync(id, ct);
            if (category is null) return Results.NotFound(new { error = "Category not found" });

            var dtos = new List<AttributeDefinitionDto>();

            // Own definitions (always included, never marked as inherited)
            foreach (var attr in category.AttributeDefinitions.OrderBy(a => a.SortOrder))
            {
                dtos.Add(new AttributeDefinitionDto(
                    attr.Id, attr.Key, attr.DisplayName,
                    attr.Target.ToString(), attr.ValueType.ToString(),
                    attr.IsFilterable, attr.IsRequired, attr.SortOrder,
                    attr.AllowedValues, IsInherited: false));
            }

            // Inherited definitions from parent chain
            if (includeInherited == true)
            {
                var ownKeys = new HashSet<string>(
                    category.AttributeDefinitions.Select(d => d.Key),
                    StringComparer.OrdinalIgnoreCase);

                var visited = new HashSet<Guid> { category.Id };
                var parentId = category.ParentCategoryId;

                while (parentId.HasValue && !visited.Contains(parentId.Value))
                {
                    var parent = await categoryRepo.GetWithAttributeDefinitionsAsync(
                        parentId.Value, ct);
                    if (parent is null) break;

                    visited.Add(parent.Id);

                    foreach (var attr in parent.AttributeDefinitions
                        .Where(d => !ownKeys.Contains(d.Key))
                        .OrderBy(a => a.SortOrder))
                    {
                        dtos.Add(new AttributeDefinitionDto(
                            attr.Id, attr.Key, attr.DisplayName,
                            attr.Target.ToString(), attr.ValueType.ToString(),
                            attr.IsFilterable, attr.IsRequired, attr.SortOrder,
                            attr.AllowedValues, IsInherited: true));
                        ownKeys.Add(attr.Key); // prevent duplicates from grandparent
                    }

                    parentId = parent.ParentCategoryId;
                }
            }

            return Results.Ok(dtos);
        })
        .WithName("GetAttributeDefinitions")
        .Produces<List<AttributeDefinitionDto>>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Authorized: remove attribute definition from category
        group.MapDelete("/{id:guid}/attributes/{attrId:guid}", async (
            Guid id,
            Guid attrId,
            ICategoryRepository categoryRepo,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var category = await categoryRepo.GetWithAttributeDefinitionsAsync(id, ct);
            if (category is null) return Results.NotFound(new { error = "Category not found" });

            // Verify attribute exists before attempting removal
            var exists = category.AttributeDefinitions.Any(a => a.Id == attrId);
            if (!exists) return Results.NotFound(new { error = "Attribute definition not found" });

            try
            {
                category.RemoveAttributeDefinition(attrId);
                categoryRepo.Update(category);
                await unitOfWork.SaveChangesAsync(ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("RemoveAttributeDefinition")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

public sealed record AddAttributeDefinitionRequest(
    string Key,
    string DisplayName,
    int Target,        // 0=Product, 1=Sku
    int ValueType,     // 0=Text, 1=Number, 2=Select
    bool IsFilterable,
    bool IsRequired,
    int SortOrder = 0,
    List<string>? AllowedValues = null);
