using Catalog.Application.Queries;
using Catalog.Domain.Entities;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Catalog.UnitTests.Application;

public class GetCategoriesQueryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly ListCategoriesHandler _handler;

    public GetCategoriesQueryHandlerTests()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _handler = new ListCategoriesHandler(_categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsActiveCategories()
    {
        // Arrange
        var query = new ListCategoriesQuery();

        var activeCategory1 = Category.Create("Category 1");
        var activeCategory2 = Category.Create("Category 2");
        var inactiveCategory = Category.Create("Category 3");
        inactiveCategory.Deactivate();

        var categories = new List<Category>
        {
            activeCategory1,
            activeCategory2,
            inactiveCategory
        };

        _categoryRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories.FindAll(c => c.IsActive)); // Assuming repo handles the filtering

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.IsActive);
        result[0].Name.Should().Be("Category 1");
        result[1].Name.Should().Be("Category 2");

        _categoryRepositoryMock.Verify(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
