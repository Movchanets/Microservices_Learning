namespace BuildingBlocks.SharedContracts.Events.StoreManagement;

public sealed record StoreVerifiedIntegrationEvent(
    Guid StoreId,
    string SellerId,
    DateTime Timestamp);
