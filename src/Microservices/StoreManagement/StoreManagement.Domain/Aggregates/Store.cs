using BuildingBlocks.SharedContracts.Abstractions;
using StoreManagement.Domain.Enumerations;
using StoreManagement.Domain.Events;

namespace StoreManagement.Domain.Aggregates;

/// <summary>
/// The Store aggregate root. Represents a seller's storefront on the marketplace.
/// Tracks verification status (Pending → Verified / Rejected) and holds store metadata
/// (name, description, logo). Sellers must have a verified store before listing products.
/// </summary>
public sealed class Store : AggregateRoot
{
    public string SellerId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public VerificationStatus VerificationStatus { get; private set; } = VerificationStatus.Pending;
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    // EF Core constructor
    private Store() { }

    public static Store Create(string sellerId, string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var store = new Store
        {
            SellerId = sellerId.Trim(),
            Name = name.Trim(),
            Description = description.Trim(),
            VerificationStatus = VerificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        store.AddDomainEvent(new StoreCreatedDomainEvent(
            store.Id, store.SellerId, store.Name));

        return store;
    }

    public void UpdateDetails(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Name = name.Trim();
        Description = description.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLogo(string logoUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logoUrl);
        LogoUrl = logoUrl.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Verify()
    {
        if (VerificationStatus == VerificationStatus.Verified)
            throw new InvalidOperationException("Store is already verified.");

        VerificationStatus = VerificationStatus.Verified;
        RejectionReason = null;
        VerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new StoreVerifiedDomainEvent(Id, SellerId));
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (VerificationStatus == VerificationStatus.Verified)
            throw new InvalidOperationException("Cannot reject a verified store.");

        VerificationStatus = VerificationStatus.Rejected;
        RejectionReason = reason.Trim();
        VerifiedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsVerified => VerificationStatus == VerificationStatus.Verified;
}
