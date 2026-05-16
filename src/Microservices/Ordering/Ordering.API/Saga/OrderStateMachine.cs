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

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // ── Correlation ────────────────────────────────────
        Event(() => OrderSubmitted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => InventoryReserved, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => InventoryFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PaymentCompleted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PaymentFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        // ── Initial → ReservingInventory ───────────────────
        Initially(
            When(OrderSubmitted)
                .Then(ctx =>
                {
                    ctx.Saga.BuyerId = ctx.Message.BuyerId;
                    ctx.Saga.OrderId = ctx.Message.CorrelationId; // CorrelationId = OrderId
                    ctx.Saga.ItemsJson = JsonSerializer.Serialize(ctx.Message.Items);
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
                    $"Inventory reservation failed: {ctx.Message.Reason}"))
                .TransitionTo(Faulted));

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
                // Compensation: release inventory
                .Publish(ctx => new CancelReservationCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    JsonSerializer.Deserialize<List<OrderItemContract>>(ctx.Saga.ItemsJson) ?? []))
                .Publish(ctx => new OrderCancelledEvent(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    ctx.Saga.BuyerId,
                    $"Payment failed: {ctx.Message.FailureReason}"))
                .TransitionTo(Cancelled));
    }
}
