using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Payment.Application.Commands.ProcessPayment;

public sealed record ProcessPaymentInternalCommand(
    Guid CorrelationId,
    Guid OrderId,
    decimal Amount,
    string BuyerId,
    bool IsSuccessful,
    string? GatewayTransactionId = null,
    string? FailureReason = null) : IRequest<Result<bool>>;
