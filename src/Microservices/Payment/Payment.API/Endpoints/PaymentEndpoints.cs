using Microsoft.AspNetCore.Mvc;
using Payment.Domain.Aggregates;

namespace Payment.API.Endpoints;

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
            [FromServices] IPaymentTransactionRepository repository,
            CancellationToken ct) =>
        {
            var transaction = await repository.GetByOrderIdAsync(orderId, ct);
            return transaction is not null
                ? Results.Ok(new
                {
                    transaction.Id,
                    transaction.OrderId,
                    transaction.Amount,
                    transaction.Status,
                    transaction.TransactionId,
                    transaction.FailureReason,
                    transaction.CreatedAt,
                    transaction.ProcessedAt
                })
                : Results.NotFound();
        });
    }
}
