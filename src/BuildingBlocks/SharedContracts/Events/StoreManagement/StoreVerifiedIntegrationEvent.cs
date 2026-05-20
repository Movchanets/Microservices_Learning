namespace BuildingBlocks.SharedContracts.Events.StoreManagement;

/// <summary>
/// Published by StoreManagement when an admin verifies/approves a seller's store.
/// Consumed by Catalog (enables product creation) and Notification (seller notification).
/// </summary>
/// <param name="StoreId">Unique identifier of the verified store.</param>
/// <param name="SellerId">Identity of the seller who owns the store.</param>
/// <param name="Timestamp">When the verification occurred.</param>
public sealed record StoreVerifiedIntegrationEvent(
    Guid StoreId,
    string SellerId,
    DateTime Timestamp);
