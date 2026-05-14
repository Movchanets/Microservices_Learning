namespace BuildingBlocks.SharedContracts.Events.Catalog;

/// <summary>
/// Published by Catalog.API when product price is specifically changed.
/// Used downstream by Inventory/Cart for price validation.
/// </summary>
public sealed record ProductPriceChangedEvent(
    Guid ProductId,
    decimal OldPrice,
    decimal NewPrice,
    string Currency,
    DateTime ChangedAt);
