using BuildingBlocks.SharedContracts.Events.Payment;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Commands.RefundPayment;
using Payment.Domain.Aggregates;

namespace Payment.API.Endpoints;

public sealed record RefundRequest(string Reason, decimal? Amount = null);

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments")
            .WithTags("Payments")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/order/{orderId:guid}", async (
            Guid orderId,
            [FromServices] IPaymentTransactionRepository transactionRepo,
            [FromServices] IRefundRepository refundRepo,
            CancellationToken ct) =>
        {
            var transaction = await transactionRepo.GetByOrderIdAsync(orderId, ct);
            if (transaction is null)
                return Results.NotFound();

            var refunds = await refundRepo.GetByOrderIdAsync(orderId, ct);

            return Results.Ok(new
            {
                transaction.Id,
                transaction.OrderId,
                transaction.Amount,
                transaction.Status,
                transaction.TransactionId,
                transaction.FailureReason,
                transaction.CreatedAt,
                transaction.ProcessedAt,
                Refunds = refunds.Select(r => new
                {
                    r.Id,
                    r.TransactionId,
                    r.Amount,
                    r.Reason,
                    r.Status,
                    r.GatewayRefundId,
                    r.CreatedAt,
                    r.ProcessedAt
                })
            });
        });

        group.MapPost("/{transactionId:guid}/refund", async (
            Guid transactionId,
            [FromBody] RefundRequest request,
            [FromServices] ISender sender,
            [FromServices] IRefundRepository refundRepo,
            [FromServices] IPublishEndpoint publishEndpoint,
            CancellationToken ct) =>
        {
            var cmd = new RefundPaymentCommand(transactionId, request.Reason, request.Amount);
            var result = await sender.Send(cmd, ct);

            if (!result.IsSuccess)
                return Results.BadRequest(new { result.Error });

            // Fetch the created refund to get full details for the integration event
            var refund = await refundRepo.GetByIdAsync(result.Value, ct);
            if (refund is not null)
            {
                await publishEndpoint.Publish(new PaymentRefundedEvent(
                    CorrelationId: refund.OrderId,
                    OrderId: refund.OrderId,
                    TransactionId: refund.TransactionId,
                    RefundId: refund.Id,
                    Amount: refund.Amount,
                    Reason: refund.Reason), ct);
            }

            return Results.Created($"/api/payments/refund/{result.Value}", new { refundId = result.Value });
        }).RequireAuthorization("Admin");
    }
}
