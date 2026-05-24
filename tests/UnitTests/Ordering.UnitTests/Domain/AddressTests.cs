// Address value object unit tests.
// Verifies constructor validation for required fields (Street, City, Country, ZipCode),
// and structural equality semantics inherited from ValueObject base class.

using FluentAssertions;
using Ordering.Domain.ValueObjects;

namespace Ordering.UnitTests.Domain;

public class AddressTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesAddress()
    {
        var address = new Address("123 Main St", "Springfield", "IL", "US", "62701");

        address.Street.Should().Be("123 Main St");
        address.City.Should().Be("Springfield");
        address.State.Should().Be("IL");
        address.Country.Should().Be("US");
        address.ZipCode.Should().Be("62701");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyStreet_Throws(string street)
    {
        var act = () => new Address(street, "City", "State", "US", "12345");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyCity_Throws(string city)
    {
        var act = () => new Address("Street", city, "State", "US", "12345");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyCountry_Throws(string country)
    {
        var act = () => new Address("Street", "City", "State", country, "12345");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyZipCode_Throws(string zip)
    {
        var act = () => new Address("Street", "City", "State", "US", zip);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a1 = new Address("123 Main St", "Springfield", "IL", "US", "62701");
        var a2 = new Address("123 Main St", "Springfield", "IL", "US", "62701");

        a1.Should().Be(a2);
        (a1 == a2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a1 = new Address("123 Main St", "Springfield", "IL", "US", "62701");
        var a2 = new Address("456 Oak Ave", "Chicago", "IL", "US", "60601");

        a1.Should().NotBe(a2);
    }
}
