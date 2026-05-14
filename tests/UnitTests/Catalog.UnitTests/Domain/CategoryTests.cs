using Catalog.Domain.Entities;
using FluentAssertions;
using System;
using Xunit;

namespace Catalog.UnitTests.Domain;

public class CategoryTests
{
    [Fact]
    public void Create_ValidInputs_GeneratesCategory()
    {
        // Arrange
        var name = "Test Category";
        var description = "Test Description";

        // Act
        var category = Category.Create(name, description);

        // Assert
        category.Should().NotBeNull();
        category.Name.Should().Be(name);
        category.Description.Should().Be(description);
        category.Slug.Should().Be("test-category"); // Generated slug
        category.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_EmptyOrNullName_ThrowsArgumentException(string? invalidName)
    {
        // Act
        Action action = () => Category.Create(invalidName!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }
}
