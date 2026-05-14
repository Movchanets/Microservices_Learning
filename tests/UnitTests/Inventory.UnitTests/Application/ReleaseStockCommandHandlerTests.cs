using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Dtos;
using FluentAssertions;
using Inventory.Application.Commands;
using Inventory.Domain.Aggregates;
using Moq;

namespace Inventory.UnitTests.Application;

public class ReleaseStockCommandHandlerTests
{
    private readonly Mock<IInventoryItemRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly ReleaseStockCommandHandler _handler;

    public ReleaseStockCommandHandlerTests()
    {
        _repositoryMock = new Mock<IInventoryItemRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _handler = new ReleaseStockCommandHandler(_repositoryMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ReleasesAndSaves()
    {
        // Arrange
        var sku1 = "SKU-1";
        var sku2 = "SKU-2";
        var item1 = InventoryItem.Create(sku1, 5);
        var item2 = InventoryItem.Create(sku2, 5);

        _repositoryMock.Setup(r => r.GetBySkusAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem> { item1, item2 });

        var command = new ReleaseStockCommand(Guid.NewGuid(), new List<OrderItemContract>
        {
            new OrderItemContract(sku1, 2),
            new OrderItemContract(sku2, 3),
            new OrderItemContract("SKU-3", 1) // Item not found, should be ignored
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        item1.AvailableQuantity.Should().Be(7);
        item2.AvailableQuantity.Should().Be(8);

        _repositoryMock.Verify(r => r.Update(item1), Times.Once);
        _repositoryMock.Verify(r => r.Update(item2), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
