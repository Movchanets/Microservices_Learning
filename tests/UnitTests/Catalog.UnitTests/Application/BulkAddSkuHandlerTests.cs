using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Application.Commands.BulkAddSku;
using Catalog.Application.DTOs;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.ValueObjects;
using Catalog.UnitTests.Domain.Builders;
using FluentAssertions;
using MassTransit;
using Moq;

namespace Catalog.UnitTests.Application;

public class BulkAddSkuHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();

    private BulkAddSkuHandler CreateHandler() => new(
        _productRepoMock.Object,
        _categoryRepoMock.Object,
        _unitOfWorkMock.Object,
        _publishEndpointMock.Object);

    // ════════════════════════════════════════════════════════════════
    // ALL FAIL → Result.Failure
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Handle_AllSkuCreationsFail_ReturnsFailure()
    {
        // Arrange — product already has a SKU for the only combination,
        // so AddSku throws InvalidOperationException (duplicate variant) for every combo.
        var builder = new CatalogDataBuilder()
            .WithCategory("Phones")
            .WithAttributeDefinition("color", "Color", AttributeTarget.Sku,
                AttributeType.Select, ["Black", "White"])
            .WithProduct("iPhone")
            .WithVariantAxes("color")
            .WithSku("IPH-BLK", 999m, new Dictionary<string, string> { ["color"] = "Black" });

        var product = builder.BuildProduct();
        var category = builder.BuildCategory();

        _productRepoMock
            .Setup(r => r.GetWithSkusAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _categoryRepoMock
            .Setup(r => r.GetWithAttributeDefinitionsAsync(product.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var handler = CreateHandler();
        var command = new BulkAddSkuCommand(
            product.Id,
            new Dictionary<string, List<string>>
            {
                ["color"] = ["Black"] // only one combo, already exists → duplicate
            },
            SkuCodePrefix: "IPH");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — the bug: currently returns Success with createdCount=0
        result.IsSuccess.Should().BeFalse("all SKU creations failed — should be a failure result");
        result.ErrorCode.Should().Be("ALL_SKUS_FAILED");
        result.Error.Should().Contain("IPH-BLK");
        result.Value.Should().BeNull("failure results carry no payload");
    }

    // ════════════════════════════════════════════════════════════════
    // PARTIAL SUCCESS → Result.Success with errors
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Handle_PartialSuccess_ReturnsSuccessWithErrors()
    {
        // Arrange — one combo already exists (fails), one is new (succeeds)
        var builder = new CatalogDataBuilder()
            .WithCategory("Shoes")
            .WithAttributeDefinition("color", "Color", AttributeTarget.Sku,
                AttributeType.Select, ["Black", "White"])
            .WithProduct("Sneakers")
            .WithVariantAxes("color")
            .WithSku("SNK-BLK", 89m, new Dictionary<string, string> { ["color"] = "Black" });

        var product = builder.BuildProduct();
        var category = builder.BuildCategory();

        _productRepoMock
            .Setup(r => r.GetWithSkusAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _categoryRepoMock
            .Setup(r => r.GetWithAttributeDefinitionsAsync(product.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new BulkAddSkuCommand(
            product.Id,
            new Dictionary<string, List<string>>
            {
                ["color"] = ["Black", "White"] // Black=duplicate, White=new
            },
            SkuCodePrefix: "SNK");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — partial success: 201 with both created and errors
        result.IsSuccess.Should().BeTrue("at least one SKU was created");
        result.Value.Should().NotBeNull();
        result.Value!.CreatedCount.Should().Be(1);
        result.Value.Errors.Should().NotBeNullOrEmpty("one combo should have failed");
        result.Value.Errors!.Count.Should().Be(1);
    }

    // ════════════════════════════════════════════════════════════════
    // ALL SUCCESS → Result.Success, no errors
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Handle_AllSkuCreationsSucceed_ReturnsSuccessWithNoErrors()
    {
        // Arrange — fresh product, no existing SKUs
        var builder = new CatalogDataBuilder()
            .WithCategory("Laptops")
            .WithAttributeDefinition("color", "Color", AttributeTarget.Sku,
                AttributeType.Select, ["Silver", "Space Gray"])
            .WithProduct("MacBook")
            .WithVariantAxes("color");

        var product = builder.BuildProduct();
        var category = builder.BuildCategory();

        _productRepoMock
            .Setup(r => r.GetWithSkusAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _categoryRepoMock
            .Setup(r => r.GetWithAttributeDefinitionsAsync(product.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new BulkAddSkuCommand(
            product.Id,
            new Dictionary<string, List<string>>
            {
                ["color"] = ["Silver", "Space Gray"]
            },
            SkuCodePrefix: "MBP");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CreatedCount.Should().Be(2);
        result.Value.Errors.Should().BeNull("all SKUs created successfully");
    }
}
