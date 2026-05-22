using System.Text.Json;
using BuildingBlocks.SharedContracts.Commands.Inventory;
using BuildingBlocks.SharedContracts.Commands.Payment;
using BuildingBlocks.SharedContracts.Dtos;
using BuildingBlocks.SharedContracts.Events.Cart;
using BuildingBlocks.SharedContracts.Events.Inventory;
using BuildingBlocks.SharedContracts.Events.Ordering;
using BuildingBlocks.SharedContracts.Events.Payment;
using MassTransit;
using Ordering.Infrastructure.Persistence;

namespace Ordering.API.Saga;

public sealed class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    // ── States ─────────────────────────────────────────────
    public State ReservingInventory { get; private set; } = null!;
    public State ProcessingPayment { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;
    public State Faulted { get; private set; } = null!;

    // ── Events ─────────────────────────────────────────────
    public Event<OrderSubmittedEvent> OrderSubmitted { get; private set; } = null!;
    public Event<InventoryReservedEvent> InventoryReserved { get; private set; } = null!;
    public Event<InventoryReservationFailedEvent> InventoryFailed { get; private set; } = null!;
    public Event<PaymentCompletedEvent> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; } = null!;
    public Event<CancelOrderEvent> CancelOrder { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // ── Correlation ────────────────────────────────────
        Event(() => OrderSubmitted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => InventoryReserved, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => InventoryFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PaymentCompleted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PaymentFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => CancelOrder, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        // ── Initial → ReservingInventory ───────────────────
        Initially(
            When(OrderSubmitted)
                .Then(ctx =>
                {
                    ctx.Saga.BuyerId = ctx.Message.BuyerId;
                    ctx.Saga.OrderId = ctx.Message.CorrelationId; // CorrelationId = OrderId
                    ctx.Saga.ItemsJson = JsonSerializer.Serialize(ctx.Message.Items);
                    ctx.Saga.TotalAmount = ctx.Message.Items.Sum(i => i.Price * i.Quantity);
                    ctx.Saga.CreatedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new ReserveInventoryCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Message.Items))
                .TransitionTo(ReservingInventory));

        // ── ReservingInventory ─────────────────────────────
        During(ReservingInventory,
            When(InventoryReserved)
                .Then(ctx =>
                {
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new ProcessPaymentCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Saga.TotalAmount,
                    ctx.Saga.BuyerId))
                // Publish status event so the Ordering projection consumer
                // can update the Order entity — this MUST be a separate event,
                // NOT ProcessPaymentCommand which is consumed by the Payment service.
                .Publish(ctx => new OrderStatusChangedEvent(
                    ctx.Saga.OrderId,
                    ctx.Saga.BuyerId,
                    "PaymentProcessing",
                    null,
                    DateTime.UtcNow))
                .TransitionTo(ProcessingPayment),

            When(InventoryFailed)
                .Then(ctx =>
                {
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new OrderCancelledEvent(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Saga.BuyerId,
                    $"Inventory reservation failed: {ctx.Message.Reason}",
                    DateTime.UtcNow))
                .TransitionTo(Faulted),

            // Buyer-initiated cancellation while inventory is being reserved
            When(CancelOrder)
                .Then(ctx =>
                {
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new CancelReservationCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    JsonSerializer.Deserialize<List<OrderItemContract>>(ctx.Saga.ItemsJson) ?? []))
                .Publish(ctx => new OrderCancelledEvent(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Saga.BuyerId,
                    ctx.Message.Reason ?? "Cancelled by buyer",
                    DateTime.UtcNow))
                .TransitionTo(Cancelled));

        // ── ProcessingPayment ──────────────────────────────
        During(ProcessingPayment,
            When(PaymentCompleted)
                .Then(ctx =>
                {
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new OrderCompletedEvent(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Saga.BuyerId))
                .TransitionTo(Completed),

            When(PaymentFailed)
                .Then(ctx =>
                {
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                // Compensation: release inventory and refund payment
                .Publish(ctx => new RefundPaymentIntegrationCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    Guid.Empty, // TransactionId — consumer will look up by OrderId
                    ctx.Saga.TotalAmount,
                    $"Payment failed: {ctx.Message.FailureReason}"))
                .Publish(ctx => new CancelReservationCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    JsonSerializer.Deserialize<List<OrderItemContract>>(ctx.Saga.ItemsJson) ?? []))
                .Publish(ctx => new OrderCancelledEvent(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Saga.BuyerId,
                    $"Payment failed: {ctx.Message.FailureReason}",
                    DateTime.UtcNow))
                .TransitionTo(Cancelled),

            // Buyer-initiated cancellation while payment is processing
            When(CancelOrder)
                .Then(ctx =>
                {
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                // Compensation: refund payment and release inventory
                .Publish(ctx => new RefundPaymentIntegrationCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    Guid.Empty, // TransactionId — consumer will look up by OrderId
                    ctx.Saga.TotalAmount,
                    ctx.Message.Reason ?? "Cancelled by buyer"))
                .Publish(ctx => new CancelReservationCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    JsonSerializer.Deserialize<List<OrderItemContract>>(ctx.Saga.ItemsJson) ?? []))
                .Publish(ctx => new OrderCancelledEvent(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Saga.BuyerId,
                    ctx.Message.Reason ?? "Cancelled by buyer",
                    DateTime.UtcNow))
                .TransitionTo(Cancelled));
    }
}
