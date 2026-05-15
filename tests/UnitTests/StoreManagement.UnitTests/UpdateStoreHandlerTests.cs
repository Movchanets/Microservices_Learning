using BuildingBlocks.SharedContracts.Abstractions;
using FluentAssertions;
using Moq;
using StoreManagement.Application.Commands.UpdateStore;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.UnitTests;

public class UpdateStoreHandlerTests
{
    private readonly Mock<IStoreRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UpdateStoreHandler _handler;

    public UpdateStoreHandlerTests()
    {
        _handler = new UpdateStoreHandler(_repositoryMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_UpdatesStore()
    {
        // Arrange
        var store = Store.Create("seller-1", "Old Name", "Old Description");
        _repositoryMock.Setup(r => r.GetByIdAsync(store.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var command = new UpdateStoreCommand(store.Id, "New Name", "New Description");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Name");
        result.Value.Description.Should().Be("New Description");
        store.Name.Should().Be("New Name");
        store.Description.Should().Be("New Description");
        _repositoryMock.Verify(r => r.Update(store), Times.Once);
    }

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsFailure()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        var command = new UpdateStoreCommand(Guid.NewGuid(), "Name", "Desc");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
