using System.Security.Claims;
using Media.API.Application.Commands.DeleteMedia;
using Media.API.Application.Commands.SetPrimaryMedia;
using Media.API.Application.Commands.UpdateGalleryOrder;
using Media.API.Application.Commands.UploadMedia;
using Media.API.Application.DTOs;
using Media.API.Application.Interfaces;
using Media.API.Application.Queries.GetGallery;
using Media.API.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Media.API.Endpoints;

public static class MediaEndpoints
{
    public static void MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/media")
            .WithTags("Media")
            .WithOpenApi();

        // ── Upload ─────────────────────────────────────────
        group.MapPost("/upload", async (
            IFormFile file,
            [FromForm] Guid targetId,
            [FromForm] string targetType,
            [FromForm] bool isPrimary,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest("No file provided.");

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            using var stream = file.OpenReadStream();
            var command = new UploadMediaCommand(
                stream,
                file.FileName,
                file.ContentType,
                targetId,
                targetType,
                isPrimary,
                userId);

            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/media/{result.Value!.Id}", result.Value)
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("UploadMedia")
        .RequireAuthorization()
        .DisableAntiforgery()
        .Produces<MediaItemDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // ── Get Gallery ────────────────────────────────────
        group.MapGet("/gallery/{targetType}/{targetId:guid}", async (
            string targetType,
            Guid targetId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetGalleryQuery(targetId, targetType), ct);
            return Results.Ok(result.Value);
        })
        .WithName("GetGallery")
        .Produces<List<MediaItemDto>>();

        // ── Get File ───────────────────────────────────────
        group.MapGet("/{mediaId:guid}", async (
            Guid mediaId,
            IMediaRepository mediaRepository,
            IMediaStorageService storageService,
            CancellationToken ct) =>
        {
            var media = await mediaRepository.GetByIdAsync(mediaId, ct);
            if (media is null) return Results.NotFound();

            var stream = await storageService.DownloadAsync(media.BlobName, ct);
            return Results.File(stream, media.ContentType);
        })
        .WithName("GetMediaFile")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ── Get Thumbnail ──────────────────────────────────
        group.MapGet("/{mediaId:guid}/thumbnail", async (
            Guid mediaId,
            IMediaRepository mediaRepository,
            IMediaStorageService storageService,
            CancellationToken ct) =>
        {
            var media = await mediaRepository.GetByIdAsync(mediaId, ct);
            if (media?.ThumbnailBlobName is null) return Results.NotFound();

            var stream = await storageService.DownloadAsync(media.ThumbnailBlobName, ct);
            return Results.File(stream, "image/jpeg");
        })
        .WithName("GetMediaThumbnail")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ── Delete ─────────────────────────────────────────
        group.MapDelete("/{mediaId:guid}", async (
            Guid mediaId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteMediaCommand(mediaId), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(new { result.Error, result.ErrorCode });
        })
        .WithName("DeleteMedia")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // ── Reorder Gallery ────────────────────────────────
        group.MapPut("/gallery/{targetType}/{targetId:guid}/reorder", async (
            string targetType,
            Guid targetId,
            List<GalleryOrderItem> items,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UpdateGalleryOrderCommand(targetId, targetType, items), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("ReorderGallery")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // ── Set Primary ────────────────────────────────────
        group.MapPut("/gallery/{targetType}/{targetId:guid}/primary/{mediaItemId:guid}", async (
            string targetType,
            Guid targetId,
            Guid mediaItemId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new SetPrimaryMediaCommand(targetId, targetType, mediaItemId), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { result.Error, result.ErrorCode });
        })
        .WithName("SetPrimaryMedia")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
