using BuildingBlocks.Infrastructure.Models;
using MediatR;
using StoreManagement.Application.DTOs;

namespace StoreManagement.Application.Queries.GetStoreById;

public sealed record GetStoreByIdQuery(Guid StoreId) : IRequest<Result<StoreDto>>;
