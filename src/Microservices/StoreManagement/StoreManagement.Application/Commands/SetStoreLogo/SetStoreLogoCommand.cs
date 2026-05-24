using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace StoreManagement.Application.Commands.SetStoreLogo;

public sealed record SetStoreLogoCommand(
    Guid StoreId,
    string LogoUrl) : IRequest<Result<Guid>>;
