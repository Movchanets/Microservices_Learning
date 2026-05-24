namespace BuildingBlocks.SharedContracts.Dtos;

/// <summary>
/// Shared data contract for a single order line item.
/// Used across integration events and commands (Cart, Inventory, Ordering, Payment).
/// </summary>
/// <param name="ProductId">Reference to the catalog product.</param>
/// <param name="Quantity">Number of units ordered.</param>
/// <param name="Price">Unit price at time of order (snapshot, not live price).</param>
/// <param name="StoreId">The store selling this product.</param>
public record OrderItemContract(
    Guid ProductId,
    int Quantity,
    decimal Price,
    Guid StoreId);
