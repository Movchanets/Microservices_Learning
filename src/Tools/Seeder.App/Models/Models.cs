using System.Text.Json.Serialization;

namespace Seeder.App.Models;

// ── Input Models (loaded from JSON seed data) ──────────────────

/// <summary>User seed data from users.json.</summary>
public record UserModel(string Email, string Password, string FirstName, string LastName, string Role);

/// <summary>Store seed data from stores.json.</summary>
public record StoreModel(string SellerEmail, string SellerPassword, string Name, string Description);

/// <summary>
/// Category seed data from categories.json.
/// Includes optional attribute definitions to create on the category.
/// ParentCategoryName links to a parent by name (resolved at runtime).
/// </summary>
public record CategoryModel(
    string Name,
    string Description = "",
    Guid? ParentCategoryId = null,
    [property: JsonPropertyName("ParentCategoryName")]
    string? ParentCategoryName = null,
    [property: JsonPropertyName("AttributeDefinitions")]
    List<AttributeDefinitionModel>? AttributeDefinitions = null);

/// <summary>
/// Attribute definition seed data for a category.
/// Declares which attributes products/SKUs in this category must or may have.
/// </summary>
public record AttributeDefinitionModel(
    string Key,
    string DisplayName,
    int Target,        // 0=Product, 1=Sku
    int ValueType,     // 0=Text, 1=Number, 2=Select
    bool IsFilterable,
    bool IsRequired,
    int SortOrder = 0,
    List<string>? AllowedValues = null,
    bool IsVariantAxis = false
);

/// <summary>
/// Product seed data from products-v2.json.
/// </summary>
public record ProductModel(
    [property: JsonPropertyName("productName")] string Name,
    [property: JsonPropertyName("categoryName")] string CategoryName,
    [property: JsonPropertyName("commonAttributes")] Dictionary<string, string>? CommonAttributes = null,
    [property: JsonPropertyName("variants")] List<SkuVariantModel>? Variants = null,
    [property: JsonPropertyName("categoryFilters")] List<string>? CategoryFilters = null,
    decimal Price = 0,
    string StoreName = "Tech Store",
    string Description = "Imported from Rozetka",
    string Currency = "UAH",
    string Sku = "",
    string[]? Tags = null,
    string ImageUrl = "",
    int InitialStock = 10
);

/// <summary>
/// Variant (SKU) data within a product.
/// </summary>
public record SkuVariantModel(
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("attributes")] Dictionary<string, string>? Attributes = null,
    [property: JsonPropertyName("galleryUrls")] List<string>? GalleryUrls = null
);

/// <summary>Breadcrumb navigation data from Rozetka product pages.</summary>
public record BreadcrumbDto(
    string Name,
    string? Url = null,
    int Position = 0
);

// ── API Response Models ────────────────────────────────────────

/// <summary>JWT token response from Identity API.</summary>
public record LoginResponse(string AccessToken);

/// <summary>Category response from Catalog API.</summary>
public record CategoryDto { public Guid Id { get; set; } public string Name { get; set; } = ""; }

/// <summary>User response from Identity API.</summary>
public record UserDto { public Guid Id { get; set; } public string Email { get; set; } = ""; public string Role { get; set; } = ""; }

/// <summary>Store response from StoreManagement API.</summary>
public record StoreDto { public Guid Id { get; set; } public string Name { get; set; } = ""; public string VerificationStatus { get; set; } = ""; }

/// <summary>Product response from Catalog API.</summary>
public record ProductDto { public Guid Id { get; set; } }

/// <summary>SKU response from Catalog API.</summary>
public record SkuResponseDto { public Guid Id { get; set; } public string SkuCode { get; set; } = ""; }

/// <summary>Product with its SKUs from Catalog API.</summary>
public record ProductWithSkusDto { public Guid Id { get; set; } public List<SkuResponseDto>? Skus { get; set; } }

/// <summary>Inventory item response from Inventory API.</summary>
public record InventoryItemDto(string Sku, int AvailableQuantity);

public static class ProductSeedData
{
    public static string ResolvePrimarySku(ProductModel product)
    {
        if (!string.IsNullOrWhiteSpace(product.Sku))
            return NormalizeSku(product.Sku);

        var firstVariantSku = product.Variants?
            .Select(v => v.Sku)
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

        return firstVariantSku is null ? "" : NormalizeSku(firstVariantSku);
    }

    public static string NormalizeSku(string sku)
        => sku.StartsWith("ROZ-", StringComparison.OrdinalIgnoreCase)
            ? sku.ToUpperInvariant()
            : $"ROZ-{sku}".ToUpperInvariant();

    public static string? ResolvePrimaryImage(ProductModel product)
        => !string.IsNullOrWhiteSpace(product.ImageUrl)
            ? product.ImageUrl
            : product.Variants?
                .SelectMany(v => v.GalleryUrls ?? [])
                .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
}
