namespace BuildingBlocks.SharedContracts.Events.Catalog;

public sealed record ProductPriceChangedEvent(
    Guid ProductId,
    decimal OldPrice,
    decimal NewPrice,
    string Currency,
    DateTime ChangedAt);
