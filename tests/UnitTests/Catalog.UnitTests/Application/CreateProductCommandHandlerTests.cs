using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Application.Commands.CreateProduct;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Catalog.UnitTests.Application;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateProductHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CreateProductHandler(
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsProductAndReturnsDto()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var command = new CreateProductCommand(
            Name: "Test Product",
            Description: "Test Description",
            CategoryId: categoryId,
            StoreId: storeId,
            Brand: "TestBrand",
            Tags: new List<string> { "tag1" },
            ImageUrl: "http://img.jpg");

        var category = Category.Create("Test Category");
        _categoryRepositoryMock
            .Setup(repo => repo.GetByIdAsync(command.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _productRepositoryMock.Setup(repo => repo.Add(It.IsAny<Product>()));
        _unitOfWorkMock.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be(command.Name);
        result.Value.CategoryId.Should().Be(categoryId);
        result.Value.StoreId.Should().Be(storeId);
        result.Value.Brand.Should().Be("TestBrand");
        result.Value.CategoryName.Should().Be("Test Category");

        _productRepositoryMock.Verify(repo => repo.Add(It.IsAny<Product>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new CreateProductCommand(
            Name: "Test Product",
            Description: "Test Description",
            CategoryId: Guid.NewGuid(),
            StoreId: Guid.NewGuid());

        _categoryRepositoryMock
            .Setup(repo => repo.GetByIdAsync(command.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");

        _productRepositoryMock.Verify(repo => repo.Add(It.IsAny<Product>()), Times.Never);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
