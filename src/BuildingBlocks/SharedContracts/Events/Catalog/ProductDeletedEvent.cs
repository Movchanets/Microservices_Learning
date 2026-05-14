namespace BuildingBlocks.SharedContracts.Events.Catalog;

/// <summary>
/// Published by Catalog.API when a product is soft-deleted.
/// Consumed by Search.API to remove the document from Elasticsearch index.
/// </summary>
public sealed record ProductDeletedEvent(
    Guid ProductId,
    DateTime DeletedAt);
