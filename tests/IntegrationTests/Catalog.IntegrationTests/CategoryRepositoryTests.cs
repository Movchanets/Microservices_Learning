using Catalog.Domain.Entities;
using Catalog.Infrastructure.Repositories;
using Catalog.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Catalog.IntegrationTests;

[Collection("Database collection")]
public class CategoryRepositoryTests
{
    private readonly CatalogDatabaseFixture _fixture;

    public CategoryRepositoryTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Add_ShouldSaveHierarchyOfCategories()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Catalog.Infrastructure.Persistence.CatalogDbContext>(scope.ServiceProvider);
        var repository = new CategoryRepository(context);

        var parentCategory = Category.Create("Electronics", "All electronic items", null, 1);

        repository.Add(parentCategory);
        await context.SaveChangesAsync();

        var childCategory = Category.Create("Smartphones", "Mobile devices", parentCategory.Id, 1);

        // Act
        repository.Add(childCategory);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Assert
        var retrievedParent = await repository.GetByIdAsync(parentCategory.Id);
        var retrievedChild = await repository.GetByIdAsync(childCategory.Id);

        retrievedParent.Should().NotBeNull();
        retrievedParent!.Name.Should().Be("Electronics");
        retrievedParent.ParentCategoryId.Should().BeNull();

        retrievedChild.Should().NotBeNull();
        retrievedChild!.Name.Should().Be("Smartphones");
        retrievedChild.ParentCategoryId.Should().Be(parentCategory.Id);
    }
}
