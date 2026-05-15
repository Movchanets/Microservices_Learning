using FluentAssertions;
using Moq;
using BuildingBlocks.SharedContracts.Abstractions;
using Ordering.Application.Commands.CreateOrder;
using Ordering.Domain.Aggregates;

namespace Ordering.UnitTests.Application;

public class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateOrderHandler _handler;

    public CreateOrderHandlerTests()
    {
        _handler = new CreateOrderHandler(_repositoryMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesOrderAndReturnsId()
    {
        var command = new CreateOrderCommand("buyer-1",
        [
            new CreateOrderItemDto("SKU-1", "Product 1", 10m, 2),
            new CreateOrderItemDto("SKU-2", "Product 2", 5m, 3)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
