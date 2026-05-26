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
    private static readonly Guid TestSkuId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestSkuId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

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
        var item1 = InventoryItem.Create(TestSkuId1, TestProductId1, "SKU-1", 10, TestStoreId);
        var item2 = InventoryItem.Create(TestSkuId2, TestProductId2, "SKU-2", 5, TestStoreId);

        _repositoryMock.Setup(r => r.GetBySkuIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(TestSkuId1) && ids.Contains(TestSkuId2)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem> { item1, item2 });

        var command = new ReserveStockCommand(Guid.NewGuid(), new List<OrderItemContract>
        {
            new(TestProductId1, TestSkuId1, "SKU-1", "Test Product", 2, 10m, TestStoreId),
            new(TestProductId2, TestSkuId2, "SKU-2", "Test Product", 3, 20m, TestStoreId)
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item1.AvailableQuantity.Should().Be(8);
        item2.AvailableQuantity.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenItemNotFound_ReturnsFailedResult()
    {
        _repositoryMock.Setup(r => r.GetBySkuIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem>());

        var command = new ReserveStockCommand(Guid.NewGuid(), new List<OrderItemContract>
        {
            new(TestProductId1, TestSkuId1, "SKU-1", "Test Product", 2, 10m, TestStoreId)
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenItemOutOfStock_ReturnsFailedResult()
    {
        var item1 = InventoryItem.Create(TestSkuId1, TestProductId1, "SKU-1", 1, TestStoreId);

        _repositoryMock.Setup(r => r.GetBySkuIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(TestSkuId1)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem> { item1 });

        var command = new ReserveStockCommand(Guid.NewGuid(), new List<OrderItemContract>
        {
            new(TestProductId1, TestSkuId1, "SKU-1", "Test Product", 2, 10m, TestStoreId)
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient stock");
    }
}
