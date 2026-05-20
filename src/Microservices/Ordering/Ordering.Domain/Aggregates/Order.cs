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

    public void AddItem(string sku, string productName, decimal unitPrice, int quantity, string? sellerId = null)
    {
        if (Status != OrderStatus.Submitted)
            throw new DomainException("Cannot add items to an order that is not in Submitted status");

        var existingItem = _items.FirstOrDefault(i => i.Sku == sku);
        if (existingItem != null)
        {
            _items.Remove(existingItem);
        }

        _items.Add(new OrderItem(sku, productName, unitPrice, quantity, sellerId));
    }

    public void MarkInventoryReserved()
    {
        if (Status != OrderStatus.Submitted)
            throw new DomainException("Can only transition to InventoryReserved from Submitted");

        Status = OrderStatus.InventoryReserved;
    }

    public void MarkPaymentProcessing()
    {
        if (Status != OrderStatus.InventoryReserved)
            throw new DomainException("Can only transition to PaymentProcessing from InventoryReserved");

        Status = OrderStatus.PaymentProcessing;
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
