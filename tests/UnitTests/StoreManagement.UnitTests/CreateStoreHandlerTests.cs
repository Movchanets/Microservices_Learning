using BuildingBlocks.SharedContracts.Abstractions;
using FluentAssertions;
using Moq;
using StoreManagement.Application.Commands.CreateStore;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.UnitTests;

public class CreateStoreHandlerTests
{
    private readonly Mock<IStoreRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateStoreHandler _handler;

    public CreateStoreHandlerTests()
    {
        _handler = new CreateStoreHandler(_repositoryMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesStore()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ExistsBySellerIdAsync("seller-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateStoreCommand("seller-1", "My Store", "A great store");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SellerId.Should().Be("seller-1");
        result.Value.Name.Should().Be("My Store");
        _repositoryMock.Verify(r => r.Add(It.IsAny<Store>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateSeller_ReturnsFailure()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ExistsBySellerIdAsync("seller-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateStoreCommand("seller-1", "My Store", "A great store");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("STORE_DUPLICATE");
        _repositoryMock.Verify(r => r.Add(It.IsAny<Store>()), Times.Never);
    }
}
