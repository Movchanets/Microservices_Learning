// GetOrderByIdHandler unit tests.
// Verifies the query handler returns an OrderDto when the order exists,
// and returns failure when the order is not found.

using FluentAssertions;
using Moq;
using Ordering.Application.Queries.GetOrderById;
using Ordering.Domain.Aggregates;

namespace Ordering.UnitTests.Application;

public class GetOrderByIdHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly GetOrderByIdHandler _handler;

    public GetOrderByIdHandlerTests()
    {
        _handler = new GetOrderByIdHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingOrder_ReturnsOrderDto()
    {
        var order = Order.Create("buyer-1");
        order.AddItem(Guid.NewGuid(), "Product", 10m, 2, Guid.Parse("33333333-3333-3333-3333-333333333333"));
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(
            new GetOrderByIdQuery(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.BuyerId.Should().Be("buyer-1");
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithNonExistentOrder_ReturnsFailure()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _handler.Handle(
            new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
