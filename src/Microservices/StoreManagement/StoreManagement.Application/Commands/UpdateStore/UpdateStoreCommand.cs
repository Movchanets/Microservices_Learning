using BuildingBlocks.Infrastructure.Models;
using MediatR;
using StoreManagement.Application.DTOs;

namespace StoreManagement.Application.Commands.UpdateStore;

public sealed record UpdateStoreCommand(
    Guid StoreId,
    string Name,
    string Description) : IRequest<Result<StoreDto>>;
