using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace StoreManagement.Application.Commands.VerifySeller;

public sealed record VerifySellerCommand(
    Guid StoreId,
    bool IsApproved,
    string? Reason) : IRequest<Result<Guid>>;
