using BuildingBlocks.Infrastructure.Models;
using Cart.Domain.Aggregates;
using MediatR;

namespace Cart.Application.Commands;

public record DeleteCartCommand(string BuyerId) : IRequest<Result<bool>>;

public sealed class DeleteCartCommandHandler(ICartRepository repository) : IRequestHandler<DeleteCartCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteCartCommand request, CancellationToken cancellationToken)
    {
        await repository.DeleteCartAsync(request.BuyerId, cancellationToken);
        return Result<bool>.Success(true);
    }
}