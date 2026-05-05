using BuildingBlocks.SharedContracts.Abstractions;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Represents a refresh token used for issuing new access tokens.
/// </summary>
public sealed class RefreshToken : ValueObject
{
    /// <summary>Gets the opaque token string.</summary>
    public string Token { get; }

    /// <summary>Gets the date and time when the token expires.</summary>
    public DateTime ExpiresAt { get; }

    /// <summary>Gets the date and time when the token was created.</summary>
    public DateTime CreatedAt { get; }

    private RefreshToken(string token, DateTime expiresAt, DateTime createdAt)
    {
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new RefreshToken with a specified lifetime.
    /// Rationale: Encapsulates the logic of determining the expiration time relative to its creation time.
    /// </summary>
    /// <param name="token">The opaque token string generated securely.</param>
    /// <param name="lifetime">The duration the token should remain valid.</param>
    /// <returns>A new <see cref="RefreshToken"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the token string is null or whitespace.</exception>
    public static RefreshToken Create(string token, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return new RefreshToken(token, DateTime.UtcNow.Add(lifetime), DateTime.UtcNow);
    }

    /// <summary>
    /// Gets a value indicating whether the refresh token has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Provides the values used for equality comparison.
    /// </summary>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Token;
        yield return ExpiresAt;
    }
}
