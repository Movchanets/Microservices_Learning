// Order aggregate unit tests.
// Tests the core domain logic of the Order entity: creation, item management,
// state machine transitions (Submitted -> InventoryReserved -> PaymentProcessing -> Completed/Cancelled/Faulted),
// domain event raising, and invariant enforcement. These tests ensure the Order aggregate
// correctly enforces business rules without any infrastructure dependencies.

using FluentAssertions;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;
using Ordering.Domain.Events;
using Ordering.Domain.Exceptions;

namespace Ordering.UnitTests.Domain;

public class OrderTests
{
    // ── Create ─────────────────────────────────────────────

    [Fact]
    public void Create_WithValidBuyerId_InitializesCorrectly()
    {
        var order = Order.Create("buyer-1");

        order.BuyerId.Should().Be("buyer-1");
        order.Status.Should().Be(OrderStatus.Submitted);
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        order.Items.Should().BeEmpty();
        order.TotalAmount.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidBuyerId_ThrowsDomainException(string? buyerId)
    {
        var act = () => Order.Create(buyerId!);

        act.Should().Throw<DomainException>()
            .WithMessage("*BuyerId*");
    }

    // ── AddItem ────────────────────────────────────────────

    private static readonly Guid TestStoreId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void AddItem_InSubmittedStatus_AddsItem()
    {
        var order = Order.Create("buyer-1");
        order.AddItem(Guid.NewGuid(), "Product 1", 10.50m, 2, TestStoreId);

        order.Items.Should().HaveCount(1);
        order.Items[0].ProductId.Should().NotBe(Guid.Empty);
        order.Items[0].UnitPrice.Should().Be(10.50m);
        order.Items[0].Quantity.Should().Be(2);
        order.TotalAmount.Should().Be(21.00m);
    }

    [Fact]
    public void AddItem_WithStoreId_PropagatesStoreId()
    {
        var order = Order.Create("buyer-1");
        order.AddItem(Guid.NewGuid(), "Product 1", 10.50m, 2, TestStoreId);

        order.Items.Should().HaveCount(1);
        order.Items[0].StoreId.Should().Be(TestStoreId);
    }

    [Fact]
    public void AddItem_WithDuplicateProduct_ReplacesExistingItem()
    {
        var productId = Guid.NewGuid();
        var order = Order.Create("buyer-1");
        order.AddItem(productId, "Product 1", 10m, 2, TestStoreId);
        order.AddItem(productId, "Product 1 Updated", 15m, 3, TestStoreId);

        order.Items.Should().HaveCount(1);
        order.Items[0].UnitPrice.Should().Be(15m);
        order.Items[0].Quantity.Should().Be(3);
    }

    [Fact]
    public void AddItem_WhenNotSubmitted_ThrowsDomainException()
    {
        var order = Order.Create("buyer-1");
        order.MarkInventoryReserved();

        var act = () => order.AddItem(Guid.NewGuid(), "Product 1", 10m, 1, TestStoreId);

        act.Should().Throw<DomainException>()
            .WithMessage("*Submitted*");
    }

    // ── State Transitions ──────────────────────────────────

    [Fact]
    public void MarkInventoryReserved_FromSubmitted_Succeeds()
    {
        var order = Order.Create("buyer-1");
        order.MarkInventoryReserved();

        order.Status.Should().Be(OrderStatus.InventoryReserved);
    }

    [Fact]
    public void MarkInventoryReserved_FromNonSubmitted_Throws()
    {
        var order = Order.Create("buyer-1");
        order.MarkInventoryReserved();

        var act = () => order.MarkInventoryReserved();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkPaymentProcessing_FromInventoryReserved_Succeeds()
    {
        var order = Order.Create("buyer-1");
        order.MarkInventoryReserved();
        order.MarkPaymentProcessing();

        order.Status.Should().Be(OrderStatus.PaymentProcessing);
    }

    [Fact]
    public void MarkPaymentProcessing_FromSubmitted_Throws()
    {
        var order = Order.Create("buyer-1");

        var act = () => order.MarkPaymentProcessing();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkCompleted_FromPaymentProcessing_SucceedsAndRaisesEvent()
    {
        var order = Order.Create("buyer-1");
        order.MarkInventoryReserved();
        order.MarkPaymentProcessing();
        order.MarkCompleted();

        order.Status.Should().Be(OrderStatus.Completed);
        order.CompletedAt.Should().NotBeNull();
        order.DomainEvents.Should().ContainSingle(e => e is OrderCompletedDomainEvent);
    }

    [Fact]
    public void MarkCompleted_FromInventoryReserved_Throws()
    {
        var order = Order.Create("buyer-1");
        order.MarkInventoryReserved();

        var act = () => order.MarkCompleted();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkCancelled_FromSubmitted_SucceedsAndRaisesEvent()
    {
        var order = Order.Create("buyer-1");
        order.MarkCancelled("user requested");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("user requested");
        order.DomainEvents.Should().ContainSingle(e => e is OrderCancelledDomainEvent);
    }

    [Fact]
    public void MarkCancelled_FromCompleted_Throws()
    {
        var order = Order.Create("buyer-1");
        order.MarkInventoryReserved();
        order.MarkPaymentProcessing();
        order.MarkCompleted();

        var act = () => order.MarkCancelled("too late");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkCancelled_FromCancelled_Throws()
    {
        var order = Order.Create("buyer-1");
        order.MarkCancelled("first");

        var act = () => order.MarkCancelled("second");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkFaulted_SetsStatusAndReason()
    {
        var order = Order.Create("buyer-1");
        order.MarkFaulted("system error");

        order.Status.Should().Be(OrderStatus.Faulted);
        order.CancellationReason.Should().Be("system error");
    }

    // ── UpdateStatus (Seller Status Transitions) ───────────

    [Fact]
    public void UpdateStatus_SubmittedToProcessing_Succeeds()
    {
        var order = Order.Create("buyer-1");
        order.UpdateStatus(OrderStatus.Processing);

        order.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public void UpdateStatus_ProcessingToShipped_Succeeds()
    {
        var order = Order.Create("buyer-1");
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);

        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void UpdateStatus_ShippedToDelivered_SucceedsAndSetsCompletedAt()
    {
        var order = Order.Create("buyer-1");
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);

        order.Status.Should().Be(OrderStatus.Delivered);
        order.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateStatus_ShippedToDelivered_RaisesOrderCompletedEvent()
    {
        var order = Order.Create("buyer-1");
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);

        order.DomainEvents.Should().Contain(e => e is OrderCompletedDomainEvent);
    }

    [Fact]
    public void UpdateStatus_SubmittedToShipped_ThrowsDomainException()
    {
        var order = Order.Create("buyer-1");

        var act = () => order.UpdateStatus(OrderStatus.Shipped);

        act.Should().Throw<DomainException>()
            .WithMessage("*Invalid status transition*");
    }

    [Fact]
    public void UpdateStatus_SubmittedToDelivered_ThrowsDomainException()
    {
        var order = Order.Create("buyer-1");

        var act = () => order.UpdateStatus(OrderStatus.Delivered);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateStatus_CompletedToProcessing_ThrowsDomainException()
    {
        var order = Order.Create("buyer-1");
        order.MarkInventoryReserved();
        order.MarkPaymentProcessing();
        order.MarkCompleted();

        var act = () => order.UpdateStatus(OrderStatus.Processing);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateStatus_CancelledToProcessing_ThrowsDomainException()
    {
        var order = Order.Create("buyer-1");
        order.MarkCancelled("cancelled");

        var act = () => order.UpdateStatus(OrderStatus.Processing);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateStatus_ValidTransition_RaisesStatusChangedEvent()
    {
        var order = Order.Create("buyer-1");
        order.UpdateStatus(OrderStatus.Processing, "Starting order");

        var evt = order.DomainEvents.OfType<OrderStatusChangedDomainEvent>().FirstOrDefault();
        evt.Should().NotBeNull();
        evt!.NewStatus.Should().Be(OrderStatus.Processing);
        evt.Notes.Should().Be("Starting order");
    }

    // ── Full Lifecycle ─────────────────────────────────────

    [Fact]
    public void FullLifecycle_SubmittedToCompleted()
    {
        var order = Order.Create("buyer-1");
        order.AddItem(Guid.NewGuid(), "Widget", 25m, 2, TestStoreId);

        order.MarkInventoryReserved();
        order.MarkPaymentProcessing();
        order.MarkCompleted();

        order.Status.Should().Be(OrderStatus.Completed);
        order.TotalAmount.Should().Be(50m);
        order.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void FullLifecycle_PaymentFailedCompensation()
    {
        var order = Order.Create("buyer-1");
        order.AddItem(Guid.NewGuid(), "Widget", 25m, 2, TestStoreId);

        order.MarkInventoryReserved();
        order.MarkPaymentProcessing();
        order.MarkCancelled("payment declined");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("payment declined");
    }
}
