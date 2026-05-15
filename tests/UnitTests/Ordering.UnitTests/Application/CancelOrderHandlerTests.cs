using FluentAssertions;
using Moq;
using BuildingBlocks.SharedContracts.Abstractions;
using Ordering.Application.Commands.CancelOrder;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;

namespace Ordering.UnitTests.Application;

public class CancelOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CancelOrderHandler _handler;

    public CancelOrderHandlerTests()
    {
        _handler = new CancelOrderHandler(_repositoryMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingOrder_CancelsAndReturnsSuccess()
    {
        var order = Order.Create("buyer-1");
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(
            new CancelOrderCommand(order.Id, "changed mind"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        _repositoryMock.Verify(r => r.Update(order), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentOrder_ReturnsFailure()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _handler.Handle(
            new CancelOrderCommand(Guid.NewGuid(), "reason"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
