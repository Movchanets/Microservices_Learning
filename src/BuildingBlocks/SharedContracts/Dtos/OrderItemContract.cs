namespace BuildingBlocks.SharedContracts.Dtos;

public record OrderItemContract(
    string Sku,
    int Quantity,
    decimal Price,
    string? ShopId = null);