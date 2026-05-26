using System.Text.Json;

namespace Catalog.Infrastructure.Persistence;

/// <summary>
/// Shared JSON serialization options for EF Core value converters and other infrastructure code.
/// Ensures consistent serialization between jsonb columns and API responses.
/// </summary>
internal static class CatalogJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
