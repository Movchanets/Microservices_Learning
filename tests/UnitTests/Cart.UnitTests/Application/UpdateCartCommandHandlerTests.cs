using Cart.Application.Commands;
using Cart.Application.Dtos;
using Cart.Domain.Aggregates;
using FluentAssertions;
using Moq;

namespace Cart.UnitTests.Application;

public class UpdateCartCommandHandlerTests
{
    private static readonly Guid StoreId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid StoreId2 = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ProductId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProductId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OldProductId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly Mock<ICartRepository> _repositoryMock;
    private readonly UpdateCartCommandHandler _handler;

    public UpdateCartCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICartRepository>();
        _handler = new UpdateCartCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldClearAndAddItemsAndSave()
    {
        Guid? buyerId = Guid.NewGuid();
        var existingCart = new ShoppingCart(buyerId);
        existingCart.AddItem(OldProductId, Guid.NewGuid(), "TEST-SKU", 1, StoreId1, 5m);

        _repositoryMock.Setup(r => r.GetOrCreateTrackedCartAsync(buyerId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCart);

        var newItems = new List<CartItemDto>
        {
            new(ProductId1, Guid.NewGuid(), "TEST-SKU", 2, 10m, StoreId1),
            new(ProductId2, Guid.NewGuid(), "TEST-SKU", 3, 20m, StoreId2)
        };
        var command = new UpdateCartCommand(buyerId, null, newItems);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Should().Contain(i => i.ProductId == ProductId1 && i.Quantity == 2 && i.StoreId == StoreId1);
        result.Value.Items.Should().Contain(i => i.ProductId == ProductId2 && i.Quantity == 3 && i.StoreId == StoreId2);
        result.Value.Items.Should().NotContain(i => i.ProductId == OldProductId);
    }
}
