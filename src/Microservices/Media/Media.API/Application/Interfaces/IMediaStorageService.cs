namespace Media.API.Application.Interfaces;

public interface IMediaStorageService
{
    Task<MediaStorageResult> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct);
    Task<Stream> DownloadAsync(string blobName, CancellationToken ct);
    Task DeleteAsync(string blobName, CancellationToken ct);
    string GetUrl(string blobName);
}

public sealed record MediaStorageResult(
    string BlobName,
    string Url,
    long SizeBytes);
