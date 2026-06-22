using FluentAssertions;
using Search.API.Services;

namespace Search.UnitTests;

public class SearchableFieldsTests
{
    [Fact]
    public void GetSearchableFields_ExcludesDescription()
    {
        // Description contains scraped marketing noise ("Apple Pay" etc.)
        // and should not be searchable — kept in document for display only.
        var fields = ElasticsearchService.GetSearchableFields();

        fields.Should().NotContain(f => f.StartsWith("description"));
    }

    [Fact]
    public void GetSearchableFields_IncludesNameWithBoost3()
    {
        var fields = ElasticsearchService.GetSearchableFields();

        fields.Should().Contain("name^3");
    }

    [Fact]
    public void GetSearchableFields_IncludesBrandWithBoost2()
    {
        // Brand is a clean normalized field — enables brand-based search
        var fields = ElasticsearchService.GetSearchableFields();

        fields.Should().Contain("brand^2");
    }

    [Fact]
    public void GetSearchableFields_IncludesTagsWithBoost2()
    {
        var fields = ElasticsearchService.GetSearchableFields();

        fields.Should().Contain("tags^2");
    }

    [Fact]
    public void GetSearchableFields_IncludesAttributesWildcardWithBoost1()
    {
        // Attribute values (color, storage, etc.) should be searchable via wildcard
        var fields = ElasticsearchService.GetSearchableFields();

        fields.Should().Contain("attributes.*^1");
    }

    [Fact]
    public void GetSearchableFields_HasExactlyFourFields()
    {
        var fields = ElasticsearchService.GetSearchableFields();

        fields.Should().HaveCount(4);
    }
}
