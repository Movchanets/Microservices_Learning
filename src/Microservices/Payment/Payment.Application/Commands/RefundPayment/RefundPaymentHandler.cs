using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using MediatR;
using Payment.Domain.Aggregates;
using Payment.Domain.Enumerations;

namespace Payment.Application.Commands.RefundPayment;

public sealed class RefundPaymentHandler(
    IPaymentTransactionRepository transactionRepo,
    IRefundRepository refundRepo,
    IUnitOfWork uow) : IRequestHandler<RefundPaymentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RefundPaymentCommand request, CancellationToken ct)
    {
        var transaction = await transactionRepo.GetByIdAsync(request.TransactionId, ct);
        if (transaction is null)
            return Result<Guid>.Failure("Transaction not found");

        if (transaction.Status != PaymentStatus.Completed)
            return Result<Guid>.Failure("Can only refund completed transactions");

        var existingRefunds = await refundRepo.GetByTransactionIdAsync(transaction.Id, ct);
        var totalRefunded = existingRefunds
            .Where(r => r.Status == RefundStatus.Processed)
            .Sum(r => r.Amount);

        var refundAmount = request.Amount ?? transaction.Amount;

        if (totalRefunded + refundAmount > transaction.Amount)
            return Result<Guid>.Failure(
                $"Refund amount ({refundAmount:C}) would exceed remaining refundable amount ({transaction.Amount - totalRefunded:C})");

        var refund = Refund.Create(transaction.Id, transaction.OrderId, refundAmount, request.Reason);

        refund.MarkProcessed($"ref_{Guid.NewGuid():N}");

        refundRepo.Add(refund);

        if (totalRefunded + refundAmount >= transaction.Amount)
            transaction.MarkRefunded();

        await uow.SaveChangesAsync(ct);

        return Result<Guid>.Success(refund.Id);
    }
}
