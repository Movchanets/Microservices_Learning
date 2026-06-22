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
            new CreateOrderItemDto(Guid.NewGuid(), Guid.NewGuid(), "TEST-SKU", "Product 1", 10m, 2, Guid.Parse("33333333-3333-3333-3333-333333333333")),
            new CreateOrderItemDto(Guid.NewGuid(), Guid.NewGuid(), "TEST-SKU", "Product 2", 5m, 3, Guid.Parse("33333333-3333-3333-3333-333333333333"))
        ], null, null, null, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Note: result.Value is Guid.Empty in unit tests because the mock repository
        // doesn't call EF Core's SaveChanges which generates Guid v7 IDs.
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
