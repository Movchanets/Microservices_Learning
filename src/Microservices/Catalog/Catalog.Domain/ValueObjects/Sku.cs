using System.Text.RegularExpressions;
using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.ValueObjects;

public sealed partial class Sku : ValueObject
{
    public string Value { get; }

    private Sku(string value) => Value = value;

    public static Sku Create(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        sku = sku.Trim().ToUpperInvariant();

        if (sku.Length < 3 || sku.Length > 50)
            throw new ArgumentException("SKU must be between 3 and 50 characters", nameof(sku));

        if (!SkuRegex().IsMatch(sku))
            throw new ArgumentException($"Invalid SKU format: {sku}. Use alphanumeric + hyphens only.", nameof(sku));

        return new Sku(sku);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9\-]{1,48}[A-Z0-9]$", RegexOptions.Compiled)]
    private static partial Regex SkuRegex();
}
