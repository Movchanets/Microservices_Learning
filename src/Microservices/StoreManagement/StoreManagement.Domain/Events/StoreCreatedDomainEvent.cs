using BuildingBlocks.SharedContracts.Abstractions;

namespace StoreManagement.Domain.Events;

public sealed record StoreCreatedDomainEvent(
    Guid StoreId,
    string SellerId,
    string StoreName) : IDomainEvent;
