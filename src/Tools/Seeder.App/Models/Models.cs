using System.Text.Json.Serialization;

namespace Seeder.App.Models;

public record UserModel(string Email, string Password, string FirstName, string LastName, string Role);
public record LoginResponse(string AccessToken);
public record StoreModel(string SellerEmail, string SellerPassword, string Name, string Description);
public record CategoryModel(string Name, string Description);
public record CategoryDto { public Guid Id { get; set; } public string Name { get; set; } = ""; }
public record UserDto { public Guid Id { get; set; } public string Email { get; set; } = ""; public string Role { get; set; } = ""; }
public record StoreDto { public Guid Id { get; set; } public string Name { get; set; } = ""; public string VerificationStatus { get; set; } = ""; }
public record ProductModel(string StoreName, string CategoryName, string Name, string Description, decimal Price, string Currency, string Sku, string[] Tags, string ImageUrl, int InitialStock);        
public record ProductDto { public Guid Id { get; set; } }
public record InventoryItemDto(string Sku, int AvailableQuantity);