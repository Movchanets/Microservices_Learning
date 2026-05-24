namespace Catalog.Application.DTOs;

public sealed record ReviewDto(
    Guid Id,
    Guid UserId,
    string UserName,
    int Rating,
    string Title,
    string Text,
    bool IsVerifiedPurchase,
    List<string> PhotoUrls,
    int HelpfulCount,
    int NotHelpfulCount,
    string? SellerResponse,
    DateTime? SellerResponseDate,
    DateTime CreatedAt);

public sealed record ReviewSummaryDto(
    double AverageRating,
    int TotalReviews,
    Dictionary<int, int> RatingDistribution);
