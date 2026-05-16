using BuildingBlocks.SharedContracts.Abstractions;

namespace Ordering.Domain.ValueObjects;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string Country { get; }
    public string ZipCode { get; }

    public Address(string street, string city, string state, string country, string zipCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        ArgumentException.ThrowIfNullOrWhiteSpace(zipCode);

        Street = street;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipCode;
    }

    public static Address? FromShipping(
        string? line1, string? line2, string? city, string? state, string? postalCode, string? country)
    {
        if (string.IsNullOrEmpty(line1) || string.IsNullOrEmpty(city) ||
            string.IsNullOrEmpty(country) || string.IsNullOrEmpty(postalCode))
            return null;

        var street = string.IsNullOrEmpty(line2) ? line1 : $"{line1} {line2}";
        return new Address(street, city, state ?? string.Empty, country, postalCode);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }
}
