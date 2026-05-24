using BuildingBlocks.Infrastructure.Models;
using Cart.Domain.Aggregates;
using MediatR;

namespace Cart.Application.Commands;

public record DeleteCartCommand(Guid? BuyerId, Guid? CartId) : IRequest<Result<bool>>;

public sealed class DeleteCartCommandHandler(ICartRepository repository) : IRequestHandler<DeleteCartCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteCartCommand request, CancellationToken cancellationToken)
    {
        await repository.DeleteCartAsync(request.BuyerId, request.CartId, cancellationToken);
        return Result<bool>.Success(true);
    }
}
