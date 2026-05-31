using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Events;

public sealed record SkuCreatedDomainEvent(
    Guid ProductId,
    Guid SkuId,
    string SkuCode,
    string ProductName,
    Guid StoreId,
    decimal Price,
    string Currency,
    Dictionary<string, string> TypedAttributes,
    Dictionary<string, string> FlexibleAttributes) : IDomainEvent;
