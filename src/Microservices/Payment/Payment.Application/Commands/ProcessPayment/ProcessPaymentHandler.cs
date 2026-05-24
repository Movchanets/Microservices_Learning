using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Payment.Domain.Aggregates;
using MediatR;

namespace Payment.Application.Commands.ProcessPayment;

public sealed class ProcessPaymentHandler(
    IPaymentTransactionRepository repository,
    IUnitOfWork uow) : IRequestHandler<ProcessPaymentInternalCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ProcessPaymentInternalCommand request, CancellationToken ct)
    {
        var transaction = PaymentTransaction.Create(request.OrderId, request.BuyerId, request.Amount);

        repository.Add(transaction);
        await uow.SaveChangesAsync(ct);

        if (request.IsSuccessful)
        {
            transaction.MarkCompleted(request.GatewayTransactionId ?? $"txn_{Guid.NewGuid():N}");
        }
        else
        {
            transaction.MarkFailed(request.FailureReason ?? "Payment failed");
        }

        repository.Update(transaction);
        await uow.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}
