using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Aggregates;

public class Review : Entity
{
    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public int Rating { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public bool IsVerifiedPurchase { get; private set; }
    public List<string> PhotoUrls { get; private set; } = [];
    public int HelpfulCount { get; private set; }
    public int NotHelpfulCount { get; private set; }
    public string? SellerResponse { get; private set; }
    public DateTime? SellerResponseDate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Review() { }

    public static Review Create(
        Guid productId,
        Guid userId,
        string userName,
        int rating,
        string title,
        string text,
        bool isVerifiedPurchase,
        List<string>? photoUrls = null)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.", nameof(rating));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text is required.", nameof(text));

        return new Review
        {
            ProductId = productId,
            UserId = userId,
            UserName = userName,
            Rating = rating,
            Title = title,
            Text = text,
            IsVerifiedPurchase = isVerifiedPurchase,
            PhotoUrls = photoUrls ?? [],
            CreatedAt = DateTime.UtcNow
        };
    }

    public void VoteHelpful()
    {
        HelpfulCount++;
    }

    public void VoteNotHelpful()
    {
        NotHelpfulCount++;
    }

    public void AddSellerResponse(string response)
    {
        SellerResponse = response;
        SellerResponseDate = DateTime.UtcNow;
    }
}
