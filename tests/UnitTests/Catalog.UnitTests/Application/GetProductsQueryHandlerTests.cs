using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Application.Queries;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Catalog.UnitTests.Application;

public class GetProductsQueryHandlerTests
{
    private readonly Mock<IProductReadRepository> _readRepositoryMock;
    private readonly ListProductsHandler _handler;

    public GetProductsQueryHandlerTests()
    {
        _readRepositoryMock = new Mock<IProductReadRepository>();
        _handler = new ListProductsHandler(_readRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedResult()
    {
        // Arrange
        var query = new ListProductsQuery(1, 10, null, null, "Test");

        var productsList = new List<ProductListDto>
        {
            new ProductListDto(System.Guid.NewGuid(), "Test Product", 10m, "USD", "SKU", "Cat", "Active", null, System.Guid.NewGuid(), System.DateTime.UtcNow)
        };

        var pagedResult = new PagedResult<ProductListDto>(productsList, 1, 1, 10);

        _readRepositoryMock
            .Setup(repo => repo.ListAsync(
                query.Page,
                query.PageSize,
                query.CategoryId,
                query.StoreId,
                query.Search,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Test Product");
        result.TotalCount.Should().Be(1);

        _readRepositoryMock.Verify(repo => repo.ListAsync(
                query.Page,
                query.PageSize,
                query.CategoryId,
                query.StoreId,
                query.Search,
                It.IsAny<CancellationToken>()), Times.Once);
    }
}
