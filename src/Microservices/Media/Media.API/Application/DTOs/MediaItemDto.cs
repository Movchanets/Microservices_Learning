namespace Media.API.Application.DTOs;

public sealed record MediaItemDto(
    Guid Id,
    string FileName,
    string ContentType,
    string Url,
    string? ThumbnailUrl,
    long SizeBytes,
    string Type,
    int SortOrder,
    bool IsPrimary,
    DateTime CreatedAt);
