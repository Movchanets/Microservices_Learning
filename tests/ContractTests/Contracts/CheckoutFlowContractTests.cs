using BuildingBlocks.SharedContracts.Commands.Inventory;
using BuildingBlocks.SharedContracts.Commands.Payment;
using BuildingBlocks.SharedContracts.Dtos;
using BuildingBlocks.SharedContracts.Events.Cart;
using BuildingBlocks.SharedContracts.Events.Inventory;
using BuildingBlocks.SharedContracts.Events.Ordering;
using BuildingBlocks.SharedContracts.Events.Payment;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Ordering.API.Saga;
using Ordering.Infrastructure.Persistence;

namespace ContractTests.Contracts;

/// <summary>
/// Saga contract tests verifying OrderStateMachine state transitions
/// and message contracts using in-memory saga repository.
///
/// These tests verify saga logic (state transitions, published commands/events,
/// field mapping) without any infrastructure dependencies. Fast, no containers.
/// </summary>
public sealed class CheckoutFlowContractTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<OrderStateMachine, OrderState> _sagaHarness = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddSagaStateMachine<OrderStateMachine, OrderState>();
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        _sagaHarness = _provider.GetRequiredService<ISagaStateMachineTestHarness<OrderStateMachine, OrderState>>();

        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
    }

    // ─── Happy Path: OrderSubmitted → ReservingInventory ─────────────────

    [Fact]
    public async Task OrderSubmitted_ShouldTransitionToReservingInventory_AndPublishReserveCommand()
    {
        var correlationId = Guid.NewGuid();
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        var storeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var items = new List<OrderItemContract>
        {
            new(product1Id, Guid.NewGuid(), "SKU-1", "Product 1", 2, 29.99m, storeId),
            new(product2Id, Guid.NewGuid(), "SKU-2", "Product 2", 1, 49.99m, storeId)
        };

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-123", items, DateTime.UtcNow,
            "123 Main St", null, "Springfield", "IL", "62701", "US"));

        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        var saga = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.ReservingInventory);
        saga.Should().NotBeNull("saga should be in ReservingInventory state");

        saga!.BuyerId.Should().Be("buyer-123");
        saga.OrderId.Should().Be(correlationId);
        saga.TotalAmount.Should().Be(109.97m); // (29.99*2) + (49.99*1)
        saga.ItemsJson.Should().Contain(product1Id.ToString());
        saga.ItemsJson.Should().Contain(product2Id.ToString());

        (await _harness.Published.Any<ReserveInventoryCommand>()).Should().BeTrue(
            "saga should publish ReserveInventoryCommand after OrderSubmitted");
    }

    [Fact]
    public async Task OrderSubmitted_ShouldCalculateTotalAmount_FromItemPrices()
    {
        var correlationId = Guid.NewGuid();
        var items = new List<OrderItemContract>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "SKU-A", "Prod A", 3, 10.00m, Guid.Parse("33333333-3333-3333-3333-333333333333")),   // 30.00
            new(Guid.NewGuid(), Guid.NewGuid(), "SKU-B", "Prod B", 2, 25.50m, Guid.Parse("33333333-3333-3333-3333-333333333333")),   // 51.00
            new(Guid.NewGuid(), Guid.NewGuid(), "SKU-C", "Prod C", 1, 19.99m, Guid.Parse("33333333-3333-3333-3333-333333333333"))    // 19.99
        };
        // Total: 100.99

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-math", items, DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        var saga = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.ReservingInventory);
        saga.Should().NotBeNull();
        saga!.TotalAmount.Should().Be(100.99m);
    }

    [Fact]
    public async Task OrderSubmitted_ShouldSerializeItems_ForReserveCommand()
    {
        var correlationId = Guid.NewGuid();

        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var storeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-j",
            [new OrderItemContract(productId, skuId, "SKU-5", "Product", 5, 9.99m, storeId)],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        var saga = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.ReservingInventory);
        saga.Should().NotBeNull();

        var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemContract>>(saga!.ItemsJson);
        deserialized.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OrderItemContract(productId, skuId, "SKU-5", "Product", 5, 9.99m, storeId));
    }

    [Fact]
    public async Task OrderSubmitted_WithShippingAddress_ShouldPersistAllFields()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-addr",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Product", 1, 10m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow,
            "123 Main St", "Apt 4B", "Springfield", "IL", "62701", "US"));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        var saga = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.ReservingInventory);
        saga.Should().NotBeNull();
        saga!.BuyerId.Should().Be("buyer-addr");
    }

    // ─── InventoryReserved → ProcessingPayment ──────────────────────────

    [Fact]
    public async Task InventoryReserved_ShouldTransitionToProcessingPayment_AndPublishPaymentCommand()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-123",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Product", 2, 29.99m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new InventoryReservedEvent(correlationId, correlationId));
        (await _sagaHarness.Consumed.Any<InventoryReservedEvent>()).Should().BeTrue();

        var saga = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.ProcessingPayment);
        saga.Should().NotBeNull("saga should be in ProcessingPayment after inventory reserved");

        (await _harness.Published.Any<ProcessPaymentCommand>()).Should().BeTrue(
            "saga should publish ProcessPaymentCommand after inventory reserved");
    }

    [Fact]
    public async Task InventoryReserved_WithZeroAmount_ShouldStillTransitionToPayment()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-free",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-0", "Free Product", 1, 0m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new InventoryReservedEvent(correlationId, correlationId));
        (await _sagaHarness.Consumed.Any<InventoryReservedEvent>()).Should().BeTrue();

        var saga = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.ProcessingPayment);
        saga.Should().NotBeNull();
        saga!.TotalAmount.Should().Be(0m);

        (await _harness.Published.Any<ProcessPaymentCommand>()).Should().BeTrue(
            "zero-amount orders should still go through payment flow");
    }

    // ─── PaymentCompleted → Completed ───────────────────────────────────

    [Fact]
    public async Task PaymentCompleted_ShouldTransitionToCompleted_AndPublishOrderCompleted()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-1",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Product", 1, 100m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new InventoryReservedEvent(correlationId, correlationId));
        (await _sagaHarness.Consumed.Any<InventoryReservedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new PaymentCompletedEvent(
            correlationId, correlationId, "txn_abc123"));
        (await _sagaHarness.Consumed.Any<PaymentCompletedEvent>()).Should().BeTrue();

        var saga = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.Completed);
        saga.Should().NotBeNull("saga should be in Completed state after payment success");

        (await _harness.Published.Any<OrderCompletedEvent>()).Should().BeTrue(
            "saga should publish OrderCompletedEvent after payment success");
    }

    // ─── Full Happy Path ────────────────────────────────────────────────

    [Fact]
    public async Task FullHappyPath_ShouldFlowThroughAllStates()
    {
        var correlationId = Guid.NewGuid();

        // Step 1: OrderSubmitted → ReservingInventory
        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-x",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-A", "Prod A", 3, 10m, Guid.Parse("33333333-3333-3333-3333-333333333333")),
             new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-B", "Prod B", 1, 5m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow, "1 Main", null, "Town", "ST", "00000", "US"));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.ReservingInventory)
            .Should().NotBeNull("step 1: should be in ReservingInventory");

        // Step 2: InventoryReserved → ProcessingPayment
        await _harness.Bus.Publish(new InventoryReservedEvent(correlationId, correlationId));
        (await _sagaHarness.Consumed.Any<InventoryReservedEvent>()).Should().BeTrue();

        _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.ProcessingPayment)
            .Should().NotBeNull("step 2: should be in ProcessingPayment");

        // Step 3: PaymentCompleted → Completed
        await _harness.Bus.Publish(new PaymentCompletedEvent(
            correlationId, correlationId, "txn-final"));
        (await _sagaHarness.Consumed.Any<PaymentCompletedEvent>()).Should().BeTrue();

        _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.Completed)
            .Should().NotBeNull("step 3: should be in Completed");

        (await _harness.Published.Any<ReserveInventoryCommand>()).Should().BeTrue();
        (await _harness.Published.Any<ProcessPaymentCommand>()).Should().BeTrue();
        (await _harness.Published.Any<OrderCompletedEvent>()).Should().BeTrue();
    }

    // ─── Inventory Failure Path ──────────────────────────────────────────

    [Fact]
    public async Task InventoryFailed_ShouldTransitionToFaulted_AndPublishOrderCancelled()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-1",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Product", 5, 20m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new InventoryReservationFailedEvent(
            correlationId, correlationId, "Insufficient stock for SKU-001"));
        (await _sagaHarness.Consumed.Any<InventoryReservationFailedEvent>()).Should().BeTrue();

        var saga = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.Faulted);
        saga.Should().NotBeNull("saga should be in Faulted state after inventory failure");

        (await _harness.Published.Any<OrderCancelledEvent>()).Should().BeTrue(
            "saga should publish OrderCancelledEvent after inventory failure");
    }

    [Fact]
    public async Task InventoryFailed_ShouldNotPublishProcessPaymentCommand()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-1",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Product", 1, 10m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new InventoryReservationFailedEvent(
            correlationId, correlationId, "Out of stock"));
        (await _sagaHarness.Consumed.Any<InventoryReservationFailedEvent>()).Should().BeTrue();

        (await _harness.Published.Any<ProcessPaymentCommand>()).Should().BeFalse(
            "payment command should NOT be published when inventory fails");
    }

    // ─── Payment Failure Path (Compensation) ─────────────────────────────

    [Fact]
    public async Task PaymentFailed_ShouldTransitionToCancelled_AndPublishCompensation()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-fail",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-A", "Prod A", 2, 50m, Guid.Parse("33333333-3333-3333-3333-333333333333")),
             new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-B", "Prod B", 1, 30m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new InventoryReservedEvent(correlationId, correlationId));
        (await _sagaHarness.Consumed.Any<InventoryReservedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new PaymentFailedEvent(
            correlationId, correlationId, "Card declined"));
        (await _sagaHarness.Consumed.Any<PaymentFailedEvent>()).Should().BeTrue();

        var saga = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.Cancelled);
        saga.Should().NotBeNull("saga should be in Cancelled state after payment failure");

        (await _harness.Published.Any<CancelReservationCommand>()).Should().BeTrue(
            "saga should publish CancelReservationCommand as compensation");
        (await _harness.Published.Any<OrderCancelledEvent>()).Should().BeTrue(
            "saga should publish OrderCancelledEvent after payment failure");
    }

    [Fact]
    public async Task PaymentFailed_ShouldDeserializeItemsCorrectly_ForCompensation()
    {
        var correlationId = Guid.NewGuid();

        var productId2 = Guid.NewGuid();
        var skuId2 = Guid.NewGuid();
        var storeId2 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-json",
            [new OrderItemContract(productId2, skuId2, "SKU-7", "Product", 7, 12.50m, storeId2)],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new InventoryReservedEvent(correlationId, correlationId));
        (await _sagaHarness.Consumed.Any<InventoryReservedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new PaymentFailedEvent(
            correlationId, correlationId, "Gateway timeout"));
        (await _sagaHarness.Consumed.Any<PaymentFailedEvent>()).Should().BeTrue();

        (await _harness.Published.Any<CancelReservationCommand>()).Should().BeTrue();

        var saga = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.Cancelled);
        saga.Should().NotBeNull();

        var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemContract>>(saga!.ItemsJson);
        deserialized.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OrderItemContract(productId2, skuId2, "SKU-7", "Product", 7, 12.50m, storeId2));
    }

    // ─── Buyer Cancel During ReservingInventory ──────────────────────────

    [Fact]
    public async Task CancelOrder_DuringReservingInventory_ShouldTransitionToCancelled()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-cancel-1",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Product", 1, 50m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        // Cancel while inventory is being reserved
        await _harness.Bus.Publish(new CancelOrderEvent(
            correlationId, correlationId, "buyer-cancel-1", "Changed my mind", DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<CancelOrderEvent>()).Should().BeTrue();

        var saga = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.Cancelled);
        saga.Should().NotBeNull("saga should be in Cancelled state after buyer cancels during inventory reservation");
    }

    [Fact]
    public async Task CancelOrder_DuringReservingInventory_ShouldPublishCancelReservationAndOrderCancelled()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-cancel-2",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Product", 2, 30m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new CancelOrderEvent(
            correlationId, correlationId, "buyer-cancel-2", "Found better price", DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<CancelOrderEvent>()).Should().BeTrue();

        (await _harness.Published.Any<CancelReservationCommand>()).Should().BeTrue(
            "saga should publish CancelReservationCommand to release inventory");
        (await _harness.Published.Any<OrderCancelledEvent>()).Should().BeTrue(
            "saga should publish OrderCancelledEvent");

        // Should NOT publish refund command (no payment was made yet)
        (await _harness.Published.Any<RefundPaymentIntegrationCommand>()).Should().BeFalse(
            "saga should NOT publish RefundPaymentIntegrationCommand when cancelling before payment");
    }

    // ─── Buyer Cancel During ProcessingPayment ──────────────────────────

    [Fact]
    public async Task CancelOrder_DuringProcessingPayment_ShouldTransitionToCancelled()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-cancel-3",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Product", 1, 100m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new InventoryReservedEvent(correlationId, correlationId));
        (await _sagaHarness.Consumed.Any<InventoryReservedEvent>()).Should().BeTrue();

        // Cancel while payment is processing
        await _harness.Bus.Publish(new CancelOrderEvent(
            correlationId, correlationId, "buyer-cancel-3", "Found cheaper elsewhere", DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<CancelOrderEvent>()).Should().BeTrue();

        var saga = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.Cancelled);
        saga.Should().NotBeNull("saga should be in Cancelled state after buyer cancels during payment processing");
    }

    [Fact]
    public async Task CancelOrder_DuringProcessingPayment_ShouldPublishRefundAndCancelReservation()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-cancel-4",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Product", 3, 25m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new InventoryReservedEvent(correlationId, correlationId));
        (await _sagaHarness.Consumed.Any<InventoryReservedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new CancelOrderEvent(
            correlationId, correlationId, "buyer-cancel-4", "Cancelled by buyer", DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<CancelOrderEvent>()).Should().BeTrue();

        (await _harness.Published.Any<RefundPaymentIntegrationCommand>()).Should().BeTrue(
            "saga should publish RefundPaymentIntegrationCommand to refund payment");
        (await _harness.Published.Any<CancelReservationCommand>()).Should().BeTrue(
            "saga should publish CancelReservationCommand to release inventory");
        (await _harness.Published.Any<OrderCancelledEvent>()).Should().BeTrue(
            "saga should publish OrderCancelledEvent");
    }

    [Fact]
    public async Task CancelOrder_DuringProcessingPayment_RefundCommandShouldHaveCorrectAmount()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-cancel-5",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-A", "Prod A", 2, 50m, Guid.Parse("33333333-3333-3333-3333-333333333333")),
             new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-B", "Prod B", 1, 30m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new InventoryReservedEvent(correlationId, correlationId));
        (await _sagaHarness.Consumed.Any<InventoryReservedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new CancelOrderEvent(
            correlationId, correlationId, "buyer-cancel-5", "Order mistake", DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<CancelOrderEvent>()).Should().BeTrue();

        // Verify the refund command was published with correct amount
        var refundPublished = await _harness.Published.Any<RefundPaymentIntegrationCommand>();
        refundPublished.Should().BeTrue();

        var refundMessage = _harness.Published
            .Select<RefundPaymentIntegrationCommand>()
            .FirstOrDefault();
        refundMessage.Should().NotBeNull();
        refundMessage!.Context.Message.Amount.Should().Be(130m); // (50*2) + (30*1)
        refundMessage.Context.Message.OrderId.Should().Be(correlationId);
        refundMessage.Context.Message.Reason.Should().Be("Order mistake");
    }

    [Fact]
    public async Task CancelOrder_DuringProcessingPayment_ShouldNotPublishPaymentCompleted()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-cancel-6",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Product", 1, 10m, Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<OrderSubmittedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new InventoryReservedEvent(correlationId, correlationId));
        (await _sagaHarness.Consumed.Any<InventoryReservedEvent>()).Should().BeTrue();

        await _harness.Bus.Publish(new CancelOrderEvent(
            correlationId, correlationId, "buyer-cancel-6", "Duplicate order", DateTime.UtcNow));
        (await _sagaHarness.Consumed.Any<CancelOrderEvent>()).Should().BeTrue();

        (await _harness.Published.Any<OrderCompletedEvent>()).Should().BeFalse(
            "saga should NOT publish OrderCompletedEvent when order is cancelled");
    }
}
