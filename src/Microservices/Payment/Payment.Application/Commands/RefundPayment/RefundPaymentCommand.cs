using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Payment.Application.Commands.RefundPayment;

public sealed record RefundPaymentCommand(
    Guid TransactionId,
    string Reason,
    decimal? Amount = null) : IRequest<Result<Guid>>;
