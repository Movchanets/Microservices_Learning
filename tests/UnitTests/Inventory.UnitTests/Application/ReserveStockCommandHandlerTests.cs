using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Dtos;
using FluentAssertions;
using Inventory.Application.Commands;
using Inventory.Domain.Aggregates;
using Moq;

namespace Inventory.UnitTests.Application;

public class ReserveStockCommandHandlerTests
{
    private readonly Mock<IInventoryItemRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly ReserveStockCommandHandler _handler;

    public ReserveStockCommandHandlerTests()
    {
        _repositoryMock = new Mock<IInventoryItemRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _handler = new ReserveStockCommandHandler(_repositoryMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAllItemsAvailable_ReservesAndSaves()
    {
        // Arrange
        var sku1 = "SKU-1";
        var sku2 = "SKU-2";
        var item1 = InventoryItem.Create(sku1, 10);
        var item2 = InventoryItem.Create(sku2, 5);

        _repositoryMock.Setup(r => r.GetBySkusAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem> { item1, item2 });

        var command = new ReserveStockCommand(Guid.NewGuid(), new List<OrderItemContract>
        {
            new OrderItemContract(sku1, 2, 10m),
            new OrderItemContract(sku2, 3, 20m)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        item1.AvailableQuantity.Should().Be(8);
        item2.AvailableQuantity.Should().Be(2);

        _repositoryMock.Verify(r => r.Update(item1), Times.Once);
        _repositoryMock.Verify(r => r.Update(item2), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenItemNotFound_ReturnsFailedResult()
    {
        // Arrange
        var sku1 = "SKU-1";

        _repositoryMock.Setup(r => r.GetBySkusAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem>());

        var command = new ReserveStockCommand(Guid.NewGuid(), new List<OrderItemContract>
        {
            new OrderItemContract(sku1, 2, 10m)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");

        _repositoryMock.Verify(r => r.Update(It.IsAny<InventoryItem>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenItemOutOfStock_ReturnsFailedResult()
    {
        // Arrange
        var sku1 = "SKU-1";
        var item1 = InventoryItem.Create(sku1, 1); // Only 1 available

        _repositoryMock.Setup(r => r.GetBySkusAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem> { item1 });

        var command = new ReserveStockCommand(Guid.NewGuid(), new List<OrderItemContract>
        {
            new OrderItemContract(sku1, 2, 10m) // Attempt to reserve 2
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient stock");

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
