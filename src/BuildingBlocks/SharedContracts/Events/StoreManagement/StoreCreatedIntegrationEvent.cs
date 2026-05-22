namespace BuildingBlocks.SharedContracts.Events.StoreManagement;

/// <summary>
/// Published by StoreManagement when a seller creates a new store.
/// Consumed by Identity to assign the StoreId to the seller's user record
/// so they can create products before the store is verified.
/// </summary>
/// <param name="StoreId">Unique identifier of the created store.</param>
/// <param name="SellerId">Identity of the seller who owns the store.</param>
/// <param name="Timestamp">When the store was created.</param>
public sealed record StoreCreatedIntegrationEvent(
    Guid StoreId,
    string SellerId,
    DateTime Timestamp);
