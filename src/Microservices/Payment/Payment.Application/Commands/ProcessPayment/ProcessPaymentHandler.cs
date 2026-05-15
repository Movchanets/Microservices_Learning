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

        // Actual gateway call would happen here (deferred to production).
        // For now, simulate success.
        var gatewayTransactionId = $"txn_{Guid.NewGuid():N}";
        transaction.MarkCompleted(gatewayTransactionId);

        repository.Update(transaction);
        await uow.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}
