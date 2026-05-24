using BuildingBlocks.SharedContracts.Abstractions;
using Ordering.Domain.Aggregates.Entities;
using Ordering.Domain.Enumerations;
using Ordering.Domain.Events;
using Ordering.Domain.Exceptions;
using Ordering.Domain.ValueObjects;

namespace Ordering.Domain.Aggregates;

public sealed class Order : AggregateRoot
{
    public string BuyerId
    {
        get => field;
        private init => field = !string.IsNullOrWhiteSpace(value)
            ? value : throw new DomainException("BuyerId required");
    }

    public OrderStatus Status { get; private set; } = OrderStatus.Submitted;
    public Address? ShippingAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? CancellationReason { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public decimal TotalAmount => _items.Sum(i => i.TotalPrice);

    private Order() { }

    public static Order Create(string buyerId, Address? shippingAddress = null, Guid? orderId = null)
    {
        return new Order
        {
            Id = orderId ?? Guid.NewGuid(),
            BuyerId = buyerId,
            ShippingAddress = shippingAddress,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity, Guid storeId)
    {
        if (Status != OrderStatus.Submitted)
            throw new DomainException("Cannot add items to an order that is not in Submitted status");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            _items.Remove(existingItem);
        }

        _items.Add(new OrderItem(productId, productName, unitPrice, quantity, storeId));
    }

    public void MarkInventoryReserved()
    {
        if (Status != OrderStatus.Submitted)
            throw new DomainException("Can only transition to InventoryReserved from Submitted");

        Status = OrderStatus.InventoryReserved;
        AddDomainEvent(new OrderStatusChangedDomainEvent(Id, BuyerId, Status, null));
    }

    public void MarkPaymentProcessing()
    {
        if (Status != OrderStatus.InventoryReserved)
            throw new DomainException("Can only transition to PaymentProcessing from InventoryReserved");

        Status = OrderStatus.PaymentProcessing;
        AddDomainEvent(new OrderStatusChangedDomainEvent(Id, BuyerId, Status, null));
    }

    public void MarkCompleted()
    {
        if (Status != OrderStatus.PaymentProcessing)
            throw new DomainException("Can only complete from PaymentProcessing");

        Status = OrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderCompletedDomainEvent(Id, BuyerId));
    }

    public void MarkCancelled(string reason)
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
            throw new DomainException($"Cannot cancel order in {Status} status");

        Status = OrderStatus.Cancelled;
        CancellationReason = reason;

        AddDomainEvent(new OrderCancelledDomainEvent(Id, BuyerId, reason));
    }

    public void MarkFaulted(string reason)
    {
        Status = OrderStatus.Faulted;
        CancellationReason = reason;
    }

    /// <summary>
    /// Fast-forwards through sequential saga states to reach the target status.
    /// Handles race conditions where projection consumers arrive out of order
    /// (e.g., PaymentProcessing arrives before InventoryReserved).
    /// Returns true if the target was reached, false if already at/past it.
    /// </summary>
    public bool FastForwardTo(OrderStatus targetStatus, string? notes = null)
    {
        if (Status == targetStatus) return false;
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Faulted) return false;

        // Terminal failure states — transition directly from any active state
        if (targetStatus == OrderStatus.Cancelled)
        {
            MarkCancelled(notes ?? "Cancelled");
            return true;
        }
        if (targetStatus == OrderStatus.Faulted)
        {
            MarkFaulted(notes ?? "Faulted");
            return true;
        }

        // Saga path: Submitted → InventoryReserved → PaymentProcessing → Completed
        if (Status == OrderStatus.Submitted && targetStatus >= OrderStatus.InventoryReserved)
            MarkInventoryReserved();

        if (Status == OrderStatus.InventoryReserved && targetStatus >= OrderStatus.PaymentProcessing)
            MarkPaymentProcessing();

        if (Status == OrderStatus.PaymentProcessing && targetStatus == OrderStatus.Completed)
            MarkCompleted();

        return Status == targetStatus;
    }

    public void UpdateStatus(OrderStatus newStatus, string? notes = null)
    {
        var valid = newStatus switch
        {
            OrderStatus.Processing when Status == OrderStatus.Submitted => true,
            OrderStatus.Shipped when Status == OrderStatus.Processing => true,
            OrderStatus.Delivered when Status == OrderStatus.Shipped => true,
            _ => false
        };

        if (!valid)
            throw new DomainException($"Invalid status transition from {Status} to {newStatus}");

        Status = newStatus;

        if (newStatus == OrderStatus.Delivered)
        {
            CompletedAt = DateTime.UtcNow;
            AddDomainEvent(new OrderCompletedDomainEvent(Id, BuyerId));
        }

        AddDomainEvent(new OrderStatusChangedDomainEvent(Id, BuyerId, newStatus, notes));
    }
}
