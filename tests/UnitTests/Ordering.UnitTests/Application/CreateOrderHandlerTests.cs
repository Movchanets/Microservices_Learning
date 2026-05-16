// CreateOrderHandler unit tests.
// Verifies the CQRS handler creates an Order aggregate, adds items from the command,
// persists via repository and UnitOfWork, and returns the new order ID.

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
        ], null, null, null, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
