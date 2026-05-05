using System.Text.RegularExpressions;
using BuildingBlocks.SharedContracts.Abstractions;

namespace Identity.Domain.ValueObjects;

public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        email = email.Trim().ToLowerInvariant();

        if (!EmailRegex().IsMatch(email))
            throw new ArgumentException($"Invalid email format: {email}", nameof(email));

        return new Email(email);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
