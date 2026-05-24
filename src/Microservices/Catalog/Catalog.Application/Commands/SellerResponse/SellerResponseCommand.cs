using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Catalog.Application.Commands.SellerResponse;

public sealed record SellerResponseCommand(
    Guid ReviewId,
    string Response) : IRequest<Result<bool>>
{
    // Set server-side from auth claims — not client-supplied
    public Guid StoreId { get; init; }
}
