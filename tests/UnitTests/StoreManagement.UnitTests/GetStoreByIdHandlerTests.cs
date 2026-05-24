using FluentAssertions;
using Moq;
using StoreManagement.Application.Queries.GetStoreById;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.UnitTests;

public class GetStoreByIdHandlerTests
{
    private readonly Mock<IStoreRepository> _repositoryMock = new();
    private readonly GetStoreByIdHandler _handler;

    public GetStoreByIdHandlerTests()
    {
        _handler = new GetStoreByIdHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_StoreExists_ReturnsStoreDto()
    {
        // Arrange
        var store = Store.Create("seller-1", "My Store", "Description");
        _repositoryMock.Setup(r => r.GetByIdAsync(store.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var query = new GetStoreByIdQuery(store.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("My Store");
        result.Value.SellerId.Should().Be("seller-1");
    }

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsFailure()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        var query = new GetStoreByIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }
}
