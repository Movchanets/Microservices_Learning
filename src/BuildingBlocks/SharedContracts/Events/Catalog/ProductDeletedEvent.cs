namespace BuildingBlocks.SharedContracts.Events.Catalog;

public sealed record ProductDeletedEvent(
    Guid ProductId,
    DateTime DeletedAt);
