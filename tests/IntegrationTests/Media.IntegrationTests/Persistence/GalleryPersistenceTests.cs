using FluentAssertions;
using Media.API.Domain.Entities;
using Media.API.Infrastructure.Persistence;
using Media.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Media.IntegrationTests.Persistence;

public class GalleryPersistenceTests
{
    private DbContextOptions<MediaDbContext> CreateNewContextOptions()
    {
        // Create a fresh service provider, and therefore a fresh 
        // InMemory database instance.
        return new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    [Fact]
    public async Task SaveGalleryEntry_WithSkuId_SavesAndRetrievesCorrectly()
    {
        // Arrange
        var options = CreateNewContextOptions();
        var targetId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var mediaItemId = Guid.NewGuid();

        using (var context = new MediaDbContext(options))
        {
            var repository = new GalleryRepository(context);

            var entry = GalleryEntry.Create(
                mediaItemId: mediaItemId,
                targetId: targetId,
                targetType: "Product",
                sortOrder: 0,
                isPrimary: true,
                skuId: skuId
            );

            repository.Add(entry);
            await context.SaveChangesAsync();
        }

        // Act & Assert
        using (var context = new MediaDbContext(options))
        {
            var repository = new GalleryRepository(context);

            var galleries = await repository.GetByTargetAsync(targetId, "PRODUCT");
            
            galleries.Should().HaveCount(1);
            var entry = galleries.First();

            entry.SkuId.Should().Be(skuId, "Because this gallery entry is specifically bound to an SKU variant of the product.");
            entry.TargetId.Should().Be(targetId, "Because it belongs to the base product.");
            entry.TargetType.Should().Be("PRODUCT");
            entry.MediaItemId.Should().Be(mediaItemId);
        }
    }
}
