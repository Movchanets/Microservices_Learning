namespace BuildingBlocks.SharedContracts.Events.Catalog;

/// <summary>
/// Published by Catalog.API when a SKU's price is changed.
/// Consumed by Cart.API to update the cached product price.
/// Consumed by Search.API to update the price in the search index.
/// </summary>
public sealed record SkuPriceChangedEvent(
    Guid ProductId,
    Guid SkuId,
    string SkuCode,
    decimal OldPrice,
    decimal NewPrice,
    string Currency,
    DateTime ChangedAt);
