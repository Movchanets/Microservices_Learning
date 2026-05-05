using BuildingBlocks.SharedContracts.Abstractions;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Stores a hashed password. The actual hashing is done in Infrastructure layer.
/// Domain only validates the hash is not empty.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    /// <summary>Gets the hashed password string.</summary>
    public string Hash { get; }

    private PasswordHash(string hash) => Hash = hash;

    /// <summary>
    /// Creates a new instance of <see cref="PasswordHash"/>.
    /// Rationale: Validates that the provided hash string is not null or empty.
    /// Real validation is deferred to the infrastructure layer, treating this strictly as a value container.
    /// </summary>
    /// <param name="hash">The hashed password string.</param>
    /// <returns>A new <see cref="PasswordHash"/> object.</returns>
    /// <exception cref="ArgumentException">Thrown when the hash is null or whitespace.</exception>
    public static PasswordHash Create(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        return new PasswordHash(hash);
    }

    /// <summary>
    /// Factory for Infrastructure layer to use after hashing a plaintext password.
    /// Rationale: Provides semantic clarity for when an external service is passing in a pre-hashed string.
    /// </summary>
    /// <param name="hashedValue">The hashed password string.</param>
    /// <returns>A new <see cref="PasswordHash"/> object.</returns>
    public static PasswordHash FromHashedValue(string hashedValue) => Create(hashedValue);

    /// <summary>
    /// Provides the values used for equality comparison.
    /// </summary>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
    }
}
