using FluentAssertions;
using Xunit;

namespace Catalog.UnitTests;

public class BaseTest
{
    [Fact]
    public void Configuration_ShouldBeValid()
    {
        // This is a simple base test to verify xUnit and FluentAssertions are configured correctly.
        true.Should().BeTrue();
    }
}