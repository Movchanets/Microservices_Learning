using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Dtos;
using FluentAssertions;
using Inventory.Application.Commands;
using Inventory.Domain.Aggregates;
using Moq;

namespace Inventory.UnitTests.Application;

public class ReserveStockCommandHandlerTests
{
    private static readonly Guid TestStoreId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TestProductId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestProductId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

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
        var item1 = InventoryItem.Create("SKU-1", 10, TestStoreId, TestProductId1);
        var item2 = InventoryItem.Create("SKU-2", 5, TestStoreId, TestProductId2);

        _repositoryMock.Setup(r => r.GetByProductIdAsync(TestProductId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item1);
        _repositoryMock.Setup(r => r.GetByProductIdAsync(TestProductId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item2);

        var command = new ReserveStockCommand(Guid.NewGuid(), new List<OrderItemContract>
        {
            new(TestProductId1, 2, 10m, TestStoreId),
            new(TestProductId2, 3, 20m, TestStoreId)
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item1.AvailableQuantity.Should().Be(8);
        item2.AvailableQuantity.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenItemNotFound_ReturnsFailedResult()
    {
        _repositoryMock.Setup(r => r.GetByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var command = new ReserveStockCommand(Guid.NewGuid(), new List<OrderItemContract>
        {
            new(TestProductId1, 2, 10m, TestStoreId)
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenItemOutOfStock_ReturnsFailedResult()
    {
        var item1 = InventoryItem.Create("SKU-1", 1, TestStoreId, TestProductId1);

        _repositoryMock.Setup(r => r.GetByProductIdAsync(TestProductId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item1);

        var command = new ReserveStockCommand(Guid.NewGuid(), new List<OrderItemContract>
        {
            new(TestProductId1, 2, 10m, TestStoreId)
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient stock");
    }
}
