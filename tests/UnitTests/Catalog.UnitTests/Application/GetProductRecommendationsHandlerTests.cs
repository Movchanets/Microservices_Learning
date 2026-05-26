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

public class GetProductRecommendationsHandlerTests
{
    private readonly Mock<IProductReadRepository> _readRepositoryMock;
    private readonly GetProductRecommendationsHandler _handler;

    public GetProductRecommendationsHandlerTests()
    {
        _readRepositoryMock = new Mock<IProductReadRepository>();
        _handler = new GetProductRecommendationsHandler(_readRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsEmptyList()
    {
        // Arrange
        var productId = System.Guid.NewGuid();
        var query = new GetProductRecommendationsQuery(productId);

        _readRepositoryMock
            .Setup(repo => repo.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProductExists_ReturnsSameCategoryProductsExcludingCurrent()
    {
        // Arrange
        var productId = System.Guid.NewGuid();
        var categoryId = System.Guid.NewGuid();
        var query = new GetProductRecommendationsQuery(productId);

        var product = new ProductDto(
            Id: productId,
            Name: "Test Product",
            Description: "Description",
            CategoryId: categoryId,
            CategoryName: "Electronics",
            Status: "Active",
            ImageUrl: null,
            Brand: null,
            StoreId: System.Guid.NewGuid(),
            Tags: [],
            Skus: [],
            CreatedAt: System.DateTime.UtcNow,
            UpdatedAt: null);

        var relatedProduct1 = new ProductListDto(
            Id: System.Guid.NewGuid(),
            Name: "Related 1",
            MinPrice: 20m,
            MaxPrice: 20m,
            Currency: "USD",
            SkuCount: 1,
            DefaultSkuId: System.Guid.NewGuid(),
            DefaultSkuCode: "TEST-SKU",
            CategoryName: "Electronics",
            Status: "Active",
            ImageUrl: null,
            StoreId: System.Guid.NewGuid(),
            CreatedAt: System.DateTime.UtcNow);

        var relatedProduct2 = new ProductListDto(
            Id: System.Guid.NewGuid(),
            Name: "Related 2",
            MinPrice: 30m,
            MaxPrice: 30m,
            Currency: "USD",
            SkuCount: 1,
            DefaultSkuId: System.Guid.NewGuid(),
            DefaultSkuCode: "TEST-SKU",
            CategoryName: "Electronics",
            Status: "Active",
            ImageUrl: null,
            StoreId: System.Guid.NewGuid(),
            CreatedAt: System.DateTime.UtcNow);

        var currentProductDup = new ProductListDto(
            Id: productId,
            Name: "Test Product",
            MinPrice: 10m,
            MaxPrice: 10m,
            Currency: "USD",
            SkuCount: 1,
            DefaultSkuId: System.Guid.NewGuid(),
            DefaultSkuCode: "TEST-SKU",
            CategoryName: "Electronics",
            Status: "Active",
            ImageUrl: null,
            StoreId: System.Guid.NewGuid(),
            CreatedAt: System.DateTime.UtcNow);

        var pagedResult = new PagedResult<ProductListDto>(
            [relatedProduct1, relatedProduct2, currentProductDup],
            3, 1, 4);

        _readRepositoryMock
            .Setup(repo => repo.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _readRepositoryMock
            .Setup(repo => repo.ListAsync(1, 4, categoryId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().NotContain(p => p.Id == productId);
        result[0].Name.Should().Be("Related 1");
        result[1].Name.Should().Be("Related 2");
    }

    [Fact]
    public async Task Handle_MoreThan3RelatedProducts_ReturnsMax3()
    {
        // Arrange
        var productId = System.Guid.NewGuid();
        var categoryId = System.Guid.NewGuid();
        var query = new GetProductRecommendationsQuery(productId);

        var product = new ProductDto(
            Id: productId,
            Name: "Test",
            Description: "Desc",
            CategoryId: categoryId,
            CategoryName: "Cat",
            Status: "Active",
            ImageUrl: null,
            Brand: null,
            StoreId: System.Guid.NewGuid(),
            Tags: [],
            Skus: [],
            CreatedAt: System.DateTime.UtcNow,
            UpdatedAt: null);

        var items = new List<ProductListDto>();
        for (int i = 0; i < 5; i++)
        {
            items.Add(new ProductListDto(
                Id: System.Guid.NewGuid(),
                Name: $"Related {i}",
                MinPrice: 10m,
                MaxPrice: 10m,
                Currency: "USD",
                SkuCount: 1,
                DefaultSkuId: System.Guid.NewGuid(),
                DefaultSkuCode: "TEST-SKU",
                CategoryName: "Cat",
                Status: "Active",
                ImageUrl: null,
                StoreId: System.Guid.NewGuid(),
                CreatedAt: System.DateTime.UtcNow));
        }

        var pagedResult = new PagedResult<ProductListDto>(items, 5, 1, 4);

        _readRepositoryMock
            .Setup(repo => repo.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _readRepositoryMock
            .Setup(repo => repo.ListAsync(1, 4, categoryId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
    }
}
