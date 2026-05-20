using System.Security.Claims;
using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Payment;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Commands.RefundPayment;
using Payment.Domain.Aggregates;

namespace Payment.API.Endpoints;

public sealed record RefundRequest(string Reason, decimal? Amount = null);

public static class PaymentEndpoints
{
    private const int MaxRefundRetries = 2;

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
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var transaction = await transactionRepo.GetByOrderIdAsync(orderId, ct);
            if (transaction is null)
                return Results.NotFound();

            // Ownership check: only the buyer who made the payment or an Admin can view it
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = user.IsInRole("Admin");
            if (!isAdmin && transaction.BuyerId != buyerId)
                return Results.Forbid();

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
            CancellationToken ct) =>
        {
            var cmd = new RefundPaymentCommand(transactionId, request.Reason, request.Amount);

            // Retry on concurrency conflict (concurrent refund requests)
            Result<Guid>? result = null;
            for (var attempt = 0; attempt <= MaxRefundRetries; attempt++)
            {
                try
                {
                    result = await sender.Send(cmd, ct);
                    break;
                }
                catch (DbUpdateConcurrencyException) when (attempt < MaxRefundRetries)
                {
                    // Concurrent refund modified data — retry with fresh read
                }
            }

            if (result is null)
                return Results.StatusCode(500);

            if (!result.IsSuccess)
                return Results.BadRequest(new { result.Error });

            return Results.Created($"/api/payments/refund/{result.Value}", new { refundId = result.Value });
        }).RequireAuthorization("Admin");
    }
}
