using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Dtos;
using FluentAssertions;
using Inventory.Application.Commands;
using Inventory.Domain.Aggregates;
using Microsoft.Extensions.Logging;
using Moq;

namespace Inventory.UnitTests.Application;

public class ReleaseStockCommandHandlerTests
{
    private static readonly Guid TestStoreId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TestProductId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TestProductId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TestProductId3 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid TestSkuId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestSkuId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestSkuId3 = Guid.Parse("33333333-3333-3333-3333-444444444444");

    private readonly Mock<IInventoryItemRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<ReleaseStockCommandHandler>> _loggerMock;
    private readonly ReleaseStockCommandHandler _handler;

    public ReleaseStockCommandHandlerTests()
    {
        _repositoryMock = new Mock<IInventoryItemRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ReleaseStockCommandHandler>>();
        _handler = new ReleaseStockCommandHandler(_repositoryMock.Object, _uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ReleasesAndSaves()
    {
        var item1 = InventoryItem.Create(TestSkuId1, TestProductId1, "SKU-1", 5, TestStoreId);
        var item2 = InventoryItem.Create(TestSkuId2, TestProductId2, "SKU-2", 5, TestStoreId);
        item1.Reserve(2);  // Available=3, Reserved=2
        item2.Reserve(3);  // Available=2, Reserved=3

        _repositoryMock.Setup(r => r.GetBySkuIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(TestSkuId1) && ids.Contains(TestSkuId2)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem> { item1, item2 });

        var command = new ReleaseStockCommand(Guid.NewGuid(), new List<OrderItemContract>
        {
            new(TestProductId1, TestSkuId1, "SKU-1", "Test Product", 2, 10m, TestStoreId),
            new(TestProductId2, TestSkuId2, "SKU-2", "Test Product", 3, 20m, TestStoreId),
            new(TestProductId3, TestSkuId3, "SKU-3", "Test Product", 1, 5m, TestStoreId) // Not found, should be ignored
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item1.AvailableQuantity.Should().Be(5);  // 5 - 2 reserved + 2 released = 5
        item2.AvailableQuantity.Should().Be(5);  // 5 - 3 reserved + 3 released = 5
    }
}
