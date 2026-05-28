using Media.API.Application.Interfaces;
using Azure.Storage.Blobs;

namespace Media.API.Infrastructure.Storage;

public sealed class AzureBlobStorageService(
    BlobServiceClient blobClient,
    ILogger<AzureBlobStorageService> logger) : IMediaStorageService
{
    private const string ContainerName = "media";

    public async Task<MediaStorageResult> UploadAsync(
        Stream stream, string fileName, string contentType, CancellationToken ct)
    {
        var containerClient = blobClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var blob = containerClient.GetBlobClient(blobName);

        stream.Position = 0;
        await blob.UploadAsync(stream, overwrite: true, cancellationToken: ct);

        var url = blob.Uri.ToString();
        logger.LogInformation("Uploaded blob {BlobName} ({ContentType})", blobName, contentType);

        return new MediaStorageResult(blobName, url, stream.Length);
    }

    public async Task<Stream> DownloadAsync(string blobName, CancellationToken ct)
    {
        var containerClient = blobClient.GetBlobContainerClient(ContainerName);
        var blob = containerClient.GetBlobClient(blobName);

        var download = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return download.Value.Content;
    }

    public async Task DeleteAsync(string blobName, CancellationToken ct)
    {
        var containerClient = blobClient.GetBlobContainerClient(ContainerName);
        var blob = containerClient.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
        logger.LogInformation("Deleted blob {BlobName}", blobName);
    }

    public string GetUrl(string blobName)
    {
        var containerClient = blobClient.GetBlobContainerClient(ContainerName);
        return containerClient.GetBlobClient(blobName).Uri.ToString();
    }
}
