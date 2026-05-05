using BuildingBlocks.SharedContracts.Abstractions;

namespace Identity.Domain.ValueObjects;

public sealed class RefreshToken : ValueObject
{
    public string Token { get; }
    public DateTime ExpiresAt { get; }
    public DateTime CreatedAt { get; }

    private RefreshToken(string token, DateTime expiresAt, DateTime createdAt)
    {
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public static RefreshToken Create(string token, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return new RefreshToken(token, DateTime.UtcNow.Add(lifetime), DateTime.UtcNow);
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Token;
        yield return ExpiresAt;
    }
}
