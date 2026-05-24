using Cart.Application.Commands;
using Cart.Application.Dtos;
using Cart.Domain.Aggregates;
using Cart.Domain.Entities;
using Cart.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Cart.UnitTests.Application;

public class AddCartItemCommandHandlerTests
{
    private static readonly Guid TestStoreId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TestProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly Mock<ICartRepository> _repositoryMock;
    private readonly Mock<IProductPriceRepository> _priceRepositoryMock;
    private readonly AddCartItemCommandHandler _handler;

    public AddCartItemCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICartRepository>();
        _priceRepositoryMock = new Mock<IProductPriceRepository>();
        _handler = new AddCartItemCommandHandler(_repositoryMock.Object, _priceRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_FirstItemToNewCart_ShouldCreateCartAndAddItem()
    {
        Guid? buyerId = Guid.NewGuid();
        var price = 29.99m;

        _priceRepositoryMock.Setup(r => r.GetByIdAsync(TestProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProductPrice.Create(TestProductId, "PROD-001", "Test Product", price, "USD", TestStoreId));

        var freshCart = new ShoppingCart(buyerId);
        _repositoryMock.Setup(r => r.GetOrCreateTrackedCartAsync(buyerId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(freshCart);

        var command = new AddCartItemCommand(buyerId, null, TestProductId, 2);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.First().ProductId.Should().Be(TestProductId);
        result.Value.Items.First().Quantity.Should().Be(2);
        result.Value.Items.First().Price.Should().Be(price);
        result.Value.Items.First().StoreId.Should().Be(TestStoreId);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        _priceRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductPrice?)null);

        var command = new AddCartItemCommand(Guid.NewGuid(), null, TestProductId, 1);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }
}
