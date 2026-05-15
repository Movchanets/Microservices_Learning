using FluentAssertions;
using Moq;
using StoreManagement.Application.Queries.ListStores;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.UnitTests;

public class ListStoresHandlerTests
{
    private readonly Mock<IStoreRepository> _repositoryMock = new();
    private readonly ListStoresHandler _handler;

    public ListStoresHandlerTests()
    {
        _handler = new ListStoresHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllStores()
    {
        // Arrange
        var stores = new List<Store>
        {
            Store.Create("seller-1", "Store 1", "Description 1"),
            Store.Create("seller-2", "Store 2", "Description 2"),
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        var query = new ListStoresQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_FilterByStatus_ReturnsFiltered()
    {
        // Arrange
        var store1 = Store.Create("seller-1", "Store 1", "Description 1");
        var store2 = Store.Create("seller-2", "Store 2", "Description 2");
        store1.Verify(); // Verified

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store> { store1, store2 });

        var query = new ListStoresQuery(Status: "Verified");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].VerificationStatus.Should().Be("Verified");
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmpty()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        var query = new ListStoresQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
