namespace Media.API.Models;

public sealed record MediaUploadResponse(
    string BlobName,
    string Url,
    string ContentType,
    long SizeBytes);
