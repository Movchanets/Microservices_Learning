using System.Text.Json.Serialization;

namespace Seeder.App.Models;

public record UserModel(string Email, string Password, string FirstName, string LastName, string Role);
public record LoginResponse(string AccessToken);
public record StoreModel(string SellerEmail, string SellerPassword, string Name, string Description);
public record CategoryModel(string Name, string Description, Guid? ParentCategoryId = null);
public record CategoryDto { public Guid Id { get; set; } public string Name { get; set; } = ""; }
public record UserDto { public Guid Id { get; set; } public string Email { get; set; } = ""; public string Role { get; set; } = ""; }
public record StoreDto { public Guid Id { get; set; } public string Name { get; set; } = ""; public string VerificationStatus { get; set; } = ""; }

public record BreadcrumbDto(
    string Name,
    string? Url = null,
    int Position = 0
);

public record VariantModel(
    string RozetkaCode,
    string Name,
    string Type,
    decimal Price,
    string ImageUrl = "",
    [property: JsonPropertyName("Gallery")] List<string>? Gallery = null
);

public record ProductModel(
    string StoreName,
    string CategoryName,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string Sku,
    string[] Tags,
    string ImageUrl,
    int InitialStock,
    string RozetkaCode = "",
    string CategoryPath = "",
    [property: JsonPropertyName("Gallery")] List<string>? Gallery = null,
    [property: JsonPropertyName("Breadcrumbs")] List<BreadcrumbDto>? Breadcrumbs = null,
    [property: JsonPropertyName("Variants")] List<VariantModel>? Variants = null
);

public record ProductDto { public Guid Id { get; set; } }
public record SkuResponseDto { public Guid Id { get; set; } public string SkuCode { get; set; } = ""; }
public record ProductWithSkusDto { public Guid Id { get; set; } public List<SkuResponseDto>? Skus { get; set; } }
public record InventoryItemDto(string Sku, int AvailableQuantity);
