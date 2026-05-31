using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Application.Commands.ChangePrice;
using Catalog.Domain.Aggregates;
using Catalog.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Catalog.UnitTests.Application;

public class UpdateProductPriceCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ChangePriceHandler _handler;

    public UpdateProductPriceCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new ChangePriceHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesSkuPriceAndReturnsSuccess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var newPrice = 20m;
        var currency = "USD";

        var product = Product.Create("Test Product", "Test Description", Guid.NewGuid(), Guid.NewGuid());
        var sku = product.AddSku("SKU-001", Money.Create(10m, "USD"), new Dictionary<string, string>());
        var skuId = sku.Id;

        var command = new ChangePriceCommand(productId, skuId, newPrice, currency);

        _productRepositoryMock
            .Setup(repo => repo.GetWithSkusAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _productRepositoryMock.Setup(repo => repo.Update(It.IsAny<Product>()));
        _unitOfWorkMock.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        var updatedSku = product.GetSku(skuId);
        updatedSku.Price.Amount.Should().Be(newPrice);
        updatedSku.Price.Currency.Should().Be(currency);

        _productRepositoryMock.Verify(repo => repo.Update(It.IsAny<Product>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new ChangePriceCommand(Guid.NewGuid(), Guid.NewGuid(), 20m, "USD");

        _productRepositoryMock
            .Setup(repo => repo.GetWithSkusAsync(command.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");

        _productRepositoryMock.Verify(repo => repo.Update(It.IsAny<Product>()), Times.Never);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
