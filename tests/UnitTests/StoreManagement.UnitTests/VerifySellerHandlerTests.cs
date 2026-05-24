using BuildingBlocks.SharedContracts.Abstractions;
using FluentAssertions;
using Moq;
using StoreManagement.Application.Commands.VerifySeller;
using StoreManagement.Domain.Aggregates;
using StoreManagement.Domain.Enumerations;

namespace StoreManagement.UnitTests;

public class VerifySellerHandlerTests
{
    private readonly Mock<IStoreRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly VerifySellerHandler _handler;

    public VerifySellerHandlerTests()
    {
        _handler = new VerifySellerHandler(_repositoryMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_Approve_VerifiesStore()
    {
        // Arrange
        var store = Store.Create("seller-1", "My Store", "Description");
        _repositoryMock.Setup(r => r.GetByIdAsync(store.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var command = new VerifySellerCommand(store.Id, IsApproved: true, Reason: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        store.VerificationStatus.Should().Be(VerificationStatus.Verified);
        _repositoryMock.Verify(r => r.Update(store), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Reject_RejectsWithReason()
    {
        // Arrange
        var store = Store.Create("seller-1", "My Store", "Description");
        _repositoryMock.Setup(r => r.GetByIdAsync(store.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var command = new VerifySellerCommand(store.Id, IsApproved: false, Reason: "Bad docs");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        store.VerificationStatus.Should().Be(VerificationStatus.Rejected);
        store.RejectionReason.Should().Be("Bad docs");
    }

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsFailure()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        var command = new VerifySellerCommand(Guid.NewGuid(), IsApproved: true, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
