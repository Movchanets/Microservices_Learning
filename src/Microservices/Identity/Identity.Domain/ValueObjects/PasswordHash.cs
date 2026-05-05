using BuildingBlocks.SharedContracts.Abstractions;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Stores a hashed password. The actual hashing is done in Infrastructure layer.
/// Domain only validates the hash is not empty.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    public string Hash { get; }

    private PasswordHash(string hash) => Hash = hash;

    public static PasswordHash Create(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        return new PasswordHash(hash);
    }

    /// <summary>
    /// Factory for Infrastructure layer to use after hashing a plaintext password.
    /// </summary>
    public static PasswordHash FromHashedValue(string hashedValue) => Create(hashedValue);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
    }
}
