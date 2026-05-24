using BuildingBlocks.SharedContracts.Abstractions;

namespace StoreManagement.Domain.Events;

public sealed record StoreVerifiedDomainEvent(
    Guid StoreId,
    string SellerId) : IDomainEvent;
