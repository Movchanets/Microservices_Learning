using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Persistence;
using Catalog.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Catalog.IntegrationTests.Persistence;

[Collection("Database collection")]
public class VariantMatrixPersistenceTests
{
    private readonly CatalogDatabaseFixture _fixture;

    public VariantMatrixPersistenceTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveAndRetrieve_ProductVariantMatrix_RetrievesDeepHierarchyWithoutCartesianExplosion()
    {
        // Arrange
        var productId = Guid.NewGuid();
        using (var scope = _fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

            var category = Category.Create($"Shoes-{Guid.NewGuid()}");
            var colorAttr = category.AddAttributeDefinition("color", "Color", AttributeTarget.Sku, AttributeType.Select, true, true, 0, ["Black", "White"]);
            var sizeAttr = category.AddAttributeDefinition("size", "Size", AttributeTarget.Sku, AttributeType.Select, true, true, 1, ["40", "41", "42"]);
            
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var product = Product.Create("Running Shoes", "Fast shoes", category.Id, Guid.NewGuid());
            product.SetVariantAxes(new[] { colorAttr.Id, sizeAttr.Id });

            var sku1 = product.AddSku("SHOE-BLK-40", Money.Create(100, "USD"), new Dictionary<string, string> { ["color"] = "Black", ["size"] = "40" });
            var sku2 = product.AddSku("SHOE-WHT-42", Money.Create(100, "USD"), new Dictionary<string, string> { ["color"] = "White", ["size"] = "42" });

            context.Products.Add(product);
            await context.SaveChangesAsync();
            
            productId = product.Id;
            
            // Now add attribute values since SkuIds are generated
            sku1.AddOrUpdateAttributeValue(colorAttr.Id, "Black");
            sku1.AddOrUpdateAttributeValue(sizeAttr.Id, "40");
            sku2.AddOrUpdateAttributeValue(colorAttr.Id, "White");
            sku2.AddOrUpdateAttributeValue(sizeAttr.Id, "42");
            
            await context.SaveChangesAsync();
        }

        // Act & Assert
        using (var scope = _fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

            // Retrieve the full variant matrix using AsSplitQuery to avoid Cartesian Explosion
            var retrievedProduct = await context.Products
                .Include(p => p.VariantAxes)
                    .ThenInclude(va => va.AttributeDefinition)
                .Include(p => p.Skus)
                    .ThenInclude(s => s.AttributeValues)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == productId);

            // Assert
            retrievedProduct.Should().NotBeNull();
            retrievedProduct!.VariantAxes.Should().HaveCount(2);
            retrievedProduct.Skus.Should().HaveCount(2);

            var sku1 = retrievedProduct.Skus.Single(s => s.SkuCode == "SHOE-BLK-40");
            sku1.AttributeValues.Should().HaveCount(2);
            sku1.TypedAttributes["color"].Should().Be("Black");

            var sku2 = retrievedProduct.Skus.Single(s => s.SkuCode == "SHOE-WHT-42");
            sku2.AttributeValues.Should().HaveCount(2);
            sku2.TypedAttributes["size"].Should().Be("42");
        }
    }
}
