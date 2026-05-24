using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Dtos;
using FluentAssertions;
using Inventory.Application.Commands;
using Inventory.Domain.Aggregates;
using Moq;

namespace Inventory.UnitTests.Application;

public class ReleaseStockCommandHandlerTests
{
    private static readonly Guid TestStoreId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TestProductId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestProductId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TestProductId3 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

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
        var item1 = InventoryItem.Create("SKU-1", 5, TestStoreId, TestProductId1);
        var item2 = InventoryItem.Create("SKU-2", 5, TestStoreId, TestProductId2);

        _repositoryMock.Setup(r => r.GetByProductIdAsync(TestProductId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item1);
        _repositoryMock.Setup(r => r.GetByProductIdAsync(TestProductId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item2);
        _repositoryMock.Setup(r => r.GetByProductIdAsync(TestProductId3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var command = new ReleaseStockCommand(Guid.NewGuid(), new List<OrderItemContract>
        {
            new(TestProductId1, 2, 10m, TestStoreId),
            new(TestProductId2, 3, 20m, TestStoreId),
            new(TestProductId3, 1, 5m, TestStoreId) // Not found, should be ignored
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item1.AvailableQuantity.Should().Be(7);
        item2.AvailableQuantity.Should().Be(8);
    }
}
