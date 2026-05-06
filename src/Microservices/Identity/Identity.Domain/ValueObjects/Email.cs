using System.Text.RegularExpressions;
using BuildingBlocks.SharedContracts.Abstractions;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Represents an email address as a value object.
/// </summary>
public sealed partial class Email : ValueObject
{
    /// <summary>
    /// Gets the normalized string value of the email address.
    /// </summary>
    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>
    /// Creates and validates a new Email instance.
    /// Rationale: Value object factory method that centralizes domain logic for email format validation and normalization.
    /// </summary>
    /// <param name="email">The raw email string to create from.</param>
    /// <returns>A validated Email value object.</returns>
    /// <exception cref="ArgumentException">Thrown when the email is null, whitespace, or improperly formatted.</exception>
    public static Email Create(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        email = email.Trim().ToLowerInvariant();

        if (!EmailRegex().IsMatch(email))
            throw new ArgumentException($"Invalid email format: {email}", nameof(email));

        return new Email(email);
    }

    /// <summary>
    /// Provides the values used for equality comparison.
    /// </summary>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// Returns the string representation of the email.
    /// </summary>
    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
