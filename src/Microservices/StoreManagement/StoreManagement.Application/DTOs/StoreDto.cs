namespace StoreManagement.Application.DTOs;

public sealed record StoreDto(
    Guid Id,
    string SellerId,
    string Name,
    string Description,
    string? LogoUrl,
    string VerificationStatus,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? VerifiedAt);

public sealed record StoreListDto(
    Guid Id,
    string Name,
    string SellerId,
    string? LogoUrl,
    string VerificationStatus,
    DateTime CreatedAt);
