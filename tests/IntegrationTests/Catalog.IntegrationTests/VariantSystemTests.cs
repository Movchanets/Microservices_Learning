using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Application.Commands.AddSku;
using Catalog.Application.Commands.BulkAddSku;
using Catalog.Application.DTOs;
using Catalog.Application.Queries;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Repositories;
using Catalog.IntegrationTests.Fixtures;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Catalog.IntegrationTests;

[Collection("Database collection")]
public class VariantSystemTests
{
    private readonly CatalogDatabaseFixture _fixture;

    public VariantSystemTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // ════════════════════════════════════════════════════════════════
    // HELPER: Create category with Select attributes + set product variant axes
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates attribute definitions on the category (without IsVariantAxis, which no longer exists)
    /// and then sets the product's variant axes to point at those attribute definitions.
    /// </summary>
    private static void SetupVariantAxes(
        Category category,
        Product product,
        params string[] attributeKeys)
    {
        var attrIds = category.AttributeDefinitions
            .Where(a => attributeKeys.Contains(a.Key, StringComparer.OrdinalIgnoreCase))
            .OrderBy(a => a.SortOrder)
            .Select(a => a.Id)
            .ToList();

        product.SetVariantAxes(attrIds);
    }

    // ════════════════════════════════════════════════════════════════
    // BULK ADD SKU — Cartesian Product
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BulkAddSku_3Colors_x_3Storage_Creates9Skus()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var productRepo = new ProductRepository(context);
        var categoryRepo = new CategoryRepository(context);
        var publishEndpoint = new Mock<IPublishEndpoint>();

        // Create category with Select attributes (variant axes are now product-level)
        var category = Category.Create($"Electronics-{Guid.NewGuid()}");
        category.AddAttributeDefinition("color", "Color", AttributeTarget.Sku,
            AttributeType.Select, true, true, 1, ["Black", "White", "Blue"]);
        category.AddAttributeDefinition("storage", "Storage", AttributeTarget.Sku,
            AttributeType.Select, true, true, 2, ["128GB", "256GB", "512GB"]);
        categoryRepo.Add(category);
        await context.SaveChangesAsync();

        // Create product and set its variant axes
        var product = Product.Create("iPhone 17", "Latest iPhone", category.Id, Guid.NewGuid());
        SetupVariantAxes(category, product, "color", "storage");
        productRepo.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var handler = new BulkAddSkuHandler(productRepo, categoryRepo, context, publishEndpoint.Object);
        var command = new BulkAddSkuCommand(
            product.Id,
            new Dictionary<string, List<string>>
            {
                ["color"] = ["Black", "White", "Blue"],
                ["storage"] = ["128GB", "256GB", "512GB"]
            },
            BasePrice: 799.00m,
            Currency: "USD",
            SkuCodePrefix: "IPH17");

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CreatedCount.Should().Be(9);
        result.Value.TotalCombinations.Should().Be(9);
        result.Value.CreatedSkus.Should().HaveCount(9);
        result.Value.Errors.Should().BeNull();

        // Verify all combinations exist
        var skuCodes = result.Value.CreatedSkus.Select(s => s.SkuCode).ToHashSet();
        skuCodes.Should().Contain("IPH17-BLK-128GB");
        skuCodes.Should().Contain("IPH17-WHT-512GB");
        skuCodes.Should().Contain("IPH17-BLU-256GB");
    }

    [Fact]
    public async Task BulkAddSku_ExcludedCombinations_AreSkipped()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var productRepo = new ProductRepository(context);
        var categoryRepo = new CategoryRepository(context);
        var publishEndpoint = new Mock<IPublishEndpoint>();

        var category = Category.Create("T-Shirts");
        category.AddAttributeDefinition("color", "Color", AttributeTarget.Sku,
            AttributeType.Select, true, true, 1, ["Red", "Blue"]);
        category.AddAttributeDefinition("size", "Size", AttributeTarget.Sku,
            AttributeType.Select, true, true, 2, ["S", "M", "L"]);
        categoryRepo.Add(category);
        await context.SaveChangesAsync();

        var product = Product.Create("Basic Tee", "A t-shirt", category.Id, Guid.NewGuid());
        SetupVariantAxes(category, product, "color", "size");
        productRepo.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act — exclude Red+L
        var handler = new BulkAddSkuHandler(productRepo, categoryRepo, context, publishEndpoint.Object);
        var command = new BulkAddSkuCommand(
            product.Id,
            new Dictionary<string, List<string>>
            {
                ["color"] = ["Red", "Blue"],
                ["size"] = ["S", "M", "L"]
            },
            BasePrice: 29.99m,
            ExcludedCombinations: ["color:Red,size:L"],
            SkuCodePrefix: "TEE");

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — 2×3=6 minus 1 excluded = 5
        result.IsSuccess.Should().BeTrue();
        result.Value!.CreatedCount.Should().Be(5);
        result.Value.TotalCombinations.Should().Be(5);
    }

    [Fact]
    public async Task BulkAddSku_InvalidValue_ReturnsError()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var productRepo = new ProductRepository(context);
        var categoryRepo = new CategoryRepository(context);
        var publishEndpoint = new Mock<IPublishEndpoint>();

        var category = Category.Create($"Shoes-{Guid.NewGuid()}");
        category.AddAttributeDefinition("color", "Color", AttributeTarget.Sku,
            AttributeType.Select, true, true, 1, ["Black", "White"]);
        categoryRepo.Add(category);
        await context.SaveChangesAsync();

        var product = Product.Create("Sneakers", "Running shoes", category.Id, Guid.NewGuid());
        SetupVariantAxes(category, product, "color");
        productRepo.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act — "Purple" is not in AllowedValues
        var handler = new BulkAddSkuHandler(productRepo, categoryRepo, context, publishEndpoint.Object);
        var command = new BulkAddSkuCommand(
            product.Id,
            new Dictionary<string, List<string>>
            {
                ["color"] = ["Black", "Purple"]
            },
            SkuCodePrefix: "SNK");

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_VALUE");
    }

    // ════════════════════════════════════════════════════════════════
    // VARIANT MATRIX QUERY
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task VariantMatrix_ReturnsAllCombinations()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var productRepo = new ProductRepository(context);
        var categoryRepo = new CategoryRepository(context);

        var category = Category.Create("Laptops");
        category.AddAttributeDefinition("color", "Color", AttributeTarget.Sku,
            AttributeType.Select, true, true, 1, ["Silver", "Space Gray"]);
        category.AddAttributeDefinition("ram", "RAM", AttributeTarget.Sku,
            AttributeType.Select, true, true, 2, ["8GB", "16GB"]);
        categoryRepo.Add(category);
        await context.SaveChangesAsync();

        var product = Product.Create("MacBook Pro", "Apple laptop", category.Id, Guid.NewGuid());
        SetupVariantAxes(category, product, "color", "ram");
        product.AddSku("MBP-SLV-8GB", Money.Create(1299, "USD"),
            new Dictionary<string, string> { ["color"] = "Silver", ["ram"] = "8GB" });
        product.AddSku("MBP-SLV-16GB", Money.Create(1499, "USD"),
            new Dictionary<string, string> { ["color"] = "Silver", ["ram"] = "16GB" });
        // Missing: Space Gray variants
        productRepo.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act — GetVariantMatrixHandler now takes only productRepository
        var handler = new GetVariantMatrixHandler(productRepo);
        var result = await handler.Handle(
            new GetVariantMatrixQuery(product.Id), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(product.Id);
        result.Axes.Should().HaveCount(2);
        result.Axes[0].Key.Should().Be("color");
        result.Axes[0].Values.Should().Contain(["Silver", "Space Gray"]);
        result.Axes[1].Key.Should().Be("ram");
        result.Axes[1].Values.Should().Contain(["8GB", "16GB"]);

        // 2×2 = 4 combinations
        result.Options.Should().HaveCount(4);

        // Silver+8GB and Silver+16GB should be available
        result.Options.Where(o => o.IsAvailable).Should().HaveCount(2);
        result.Options.Where(o => !o.IsAvailable).Should().HaveCount(2);

        // Space Gray variants should be unavailable
        var spaceGray8Gb = result.Options.First(o =>
            o.Combination["color"] == "Space Gray" && o.Combination["ram"] == "8GB");
        spaceGray8Gb.IsAvailable.Should().BeFalse();
        spaceGray8Gb.SkuId.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════
    // SINGLE ADD SKU — Select Validation + Variant Uniqueness
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddSku_SelectValueNotInAllowedValues_ReturnsValidationError()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var productRepo = new ProductRepository(context);
        var categoryRepo = new CategoryRepository(context);
        var publishEndpoint = new Mock<IPublishEndpoint>();

        var category = Category.Create("Watches");
        category.AddAttributeDefinition("color", "Color", AttributeTarget.Sku,
            AttributeType.Select, true, true, 1, ["Gold", "Silver"]);
        categoryRepo.Add(category);
        await context.SaveChangesAsync();

        var product = Product.Create("Smart Watch", "Fitness tracker", category.Id, Guid.NewGuid());
        SetupVariantAxes(category, product, "color");
        productRepo.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act — "Rose Gold" is not in AllowedValues
        var handler = new AddSkuHandler(productRepo, categoryRepo, context, publishEndpoint.Object, NullLogger<AddSkuHandler>.Instance);
        var command = new AddSkuCommand(
            product.Id, "WATCH-ROSEGOLD", 299.99m, "USD",
            new Dictionary<string, string> { ["color"] = "Rose Gold" });

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task AddSku_DuplicateVariantCombination_ReturnsDuplicateError()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var productRepo = new ProductRepository(context);
        var categoryRepo = new CategoryRepository(context);
        var publishEndpoint = new Mock<IPublishEndpoint>();

        var category = Category.Create("Bags");
        category.AddAttributeDefinition("color", "Color", AttributeTarget.Sku,
            AttributeType.Select, true, true, 1, ["Black", "Brown"]);
        category.AddAttributeDefinition("size", "Size", AttributeTarget.Sku,
            AttributeType.Select, true, true, 2, ["Small", "Large"]);
        categoryRepo.Add(category);
        await context.SaveChangesAsync();

        var product = Product.Create("Leather Bag", "Premium leather", category.Id, Guid.NewGuid());
        SetupVariantAxes(category, product, "color", "size");
        product.AddSku("BAG-BLK-SM", Money.Create(199, "USD"),
            new Dictionary<string, string> { ["color"] = "Black", ["size"] = "Small" });
        productRepo.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act — try to add duplicate (Black, Small) with different SKU code
        var handler = new AddSkuHandler(productRepo, categoryRepo, context, publishEndpoint.Object, NullLogger<AddSkuHandler>.Instance);
        var command = new AddSkuCommand(
            product.Id, "BAG-BLK-SM-2", 199m, "USD",
            new Dictionary<string, string> { ["color"] = "Black", ["size"] = "Small" });

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_VARIANT");
    }

    [Fact]
    public async Task AddSku_DifferentVariantCombination_Succeeds()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var productRepo = new ProductRepository(context);
        var categoryRepo = new CategoryRepository(context);
        var publishEndpoint = new Mock<IPublishEndpoint>();

        var category = Category.Create("Hats");
        category.AddAttributeDefinition("color", "Color", AttributeTarget.Sku,
            AttributeType.Select, true, true, 1, ["Black", "White"]);
        category.AddAttributeDefinition("size", "Size", AttributeTarget.Sku,
            AttributeType.Select, true, true, 2, ["S", "M"]);
        categoryRepo.Add(category);
        await context.SaveChangesAsync();

        var product = Product.Create("Baseball Cap", "Classic cap", category.Id, Guid.NewGuid());
        SetupVariantAxes(category, product, "color", "size");
        product.AddSku("CAP-BLK-S", Money.Create(25, "USD"),
            new Dictionary<string, string> { ["color"] = "Black", ["size"] = "S" });
        productRepo.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act — add different combo (White, M)
        var handler = new AddSkuHandler(productRepo, categoryRepo, context, publishEndpoint.Object, NullLogger<AddSkuHandler>.Instance);
        var command = new AddSkuCommand(
            product.Id, "CAP-WHT-M", 25m, "USD",
            new Dictionary<string, string> { ["color"] = "White", ["size"] = "M" });

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SkuCode.Should().Be("CAP-WHT-M");
    }
}
