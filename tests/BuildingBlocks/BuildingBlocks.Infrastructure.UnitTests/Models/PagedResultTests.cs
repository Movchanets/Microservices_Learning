using BuildingBlocks.Infrastructure.Models;

namespace BuildingBlocks.Infrastructure.UnitTests.Models;

public class PagedResultTests
{
    [Theory]
    [InlineData(10, 5, 2)]
    [InlineData(10, 3, 4)]
    [InlineData(10, 10, 1)]
    [InlineData(0, 10, 0)]
    [InlineData(5, 10, 1)]
    public void TotalPages_ShouldBeCalculatedCorrectly(int totalCount, int pageSize, int expectedTotalPages)
    {
        // Arrange
        var items = new List<int>();

        // Act
        var result = new PagedResult<int>(items, totalCount, 1, pageSize);

        // Assert
        Assert.Equal(expectedTotalPages, result.TotalPages);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void HasPrevious_ShouldBeCorrect(int page, bool expectedHasPrevious)
    {
        // Act
        var result = new PagedResult<int>(new List<int>(), 10, page, 5);

        // Assert
        Assert.Equal(expectedHasPrevious, result.HasPrevious);
    }

    [Theory]
    [InlineData(1, 10, 5, true)]  // Page 1 of 2
    [InlineData(2, 10, 5, false)] // Page 2 of 2
    [InlineData(1, 5, 10, false)] // Page 1 of 1
    public void HasNext_ShouldBeCorrect(int page, int totalCount, int pageSize, bool expectedHasNext)
    {
        // Act
        var result = new PagedResult<int>(new List<int>(), totalCount, page, pageSize);

        // Assert
        Assert.Equal(expectedHasNext, result.HasNext);
    }

    [Fact]
    public void Properties_ShouldMatchConstructorInputs()
    {
        // Arrange
        var items = new List<string> { "item1", "item2" };
        var totalCount = 10;
        var page = 1;
        var pageSize = 2;

        // Act
        var result = new PagedResult<string>(items, totalCount, page, pageSize);

        // Assert
        Assert.Equal(items, result.Items);
        Assert.Equal(totalCount, result.TotalCount);
        Assert.Equal(page, result.Page);
        Assert.Equal(pageSize, result.PageSize);
    }
}
