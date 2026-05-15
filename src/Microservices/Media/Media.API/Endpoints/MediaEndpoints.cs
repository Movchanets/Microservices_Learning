using Azure.Storage.Blobs;
using Media.API.Models;
using Media.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Media.API.Endpoints;

public static class MediaEndpoints
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf"
    ];

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public static void MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/media")
            .WithTags("Media")
            .WithOpenApi();

        // Upload file
        group.MapPost("/upload", async (
            IFormFile file,
            [FromServices] BlobServiceClient blobClient,
            [FromServices] ImageProcessingService imageService,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest("No file provided.");

            if (file.Length > MaxFileSizeBytes)
                return Results.BadRequest($"File size exceeds maximum of {MaxFileSizeBytes / 1024 / 1024}MB.");

            if (!AllowedContentTypes.Contains(file.ContentType))
                return Results.BadRequest($"Content type '{file.ContentType}' is not allowed.");

            var containerClient = blobClient.GetBlobContainerClient("media");
            await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

            var blobName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blob = containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, overwrite: true, cancellationToken: ct);

            // Generate thumbnail for images
            if (file.ContentType.StartsWith("image/"))
            {
                stream.Position = 0;
                var thumbnailName = $"thumb_{blobName}";
                var thumbStream = await imageService.CreateThumbnailAsync(stream, ct);
                var thumbBlob = containerClient.GetBlobClient(thumbnailName);
                await thumbBlob.UploadAsync(thumbStream, overwrite: true, cancellationToken: ct);
            }

            return Results.Created(
                $"/api/media/{blobName}",
                new MediaUploadResponse(blobName, blob.Uri.ToString(), file.ContentType, file.Length));
        })
        .WithName("UploadMedia")
        .RequireAuthorization()
        .DisableAntiforgery()
        .Produces<MediaUploadResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Retrieve file
        group.MapGet("/{blobName}", async (
            string blobName,
            [FromServices] BlobServiceClient blobClient,
            CancellationToken ct) =>
        {
            var containerClient = blobClient.GetBlobContainerClient("media");
            var blob = containerClient.GetBlobClient(blobName);

            if (!await blob.ExistsAsync(ct))
                return Results.NotFound();

            var download = await blob.DownloadStreamingAsync(cancellationToken: ct);
            var contentType = download.Value.Details.ContentType ?? "application/octet-stream";
            return Results.File(download.Value.Content, contentType);
        })
        .WithName("GetMedia")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Retrieve thumbnail
        group.MapGet("/{blobName}/thumbnail", async (
            string blobName,
            [FromServices] BlobServiceClient blobClient,
            CancellationToken ct) =>
        {
            var containerClient = blobClient.GetBlobContainerClient("media");
            var thumbBlob = containerClient.GetBlobClient($"thumb_{blobName}");

            if (!await thumbBlob.ExistsAsync(ct))
                return Results.NotFound();

            var download = await thumbBlob.DownloadStreamingAsync(cancellationToken: ct);
            return Results.File(download.Value.Content, "image/jpeg");
        })
        .WithName("GetMediaThumbnail")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Delete file
        group.MapDelete("/{blobName}", async (
            string blobName,
            [FromServices] BlobServiceClient blobClient,
            CancellationToken ct) =>
        {
            var containerClient = blobClient.GetBlobContainerClient("media");
            var blob = containerClient.GetBlobClient(blobName);

            await blob.DeleteIfExistsAsync(cancellationToken: ct);

            // Also delete thumbnail if exists
            var thumbBlob = containerClient.GetBlobClient($"thumb_{blobName}");
            await thumbBlob.DeleteIfExistsAsync(cancellationToken: ct);

            return Results.NoContent();
        })
        .WithName("DeleteMedia")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent);
    }
}
