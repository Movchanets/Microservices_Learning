using BuildingBlocks.Infrastructure.Models;
using MediatR;
using StoreManagement.Application.DTOs;

namespace StoreManagement.Application.Queries.GetStoreBySellerId;

public sealed record GetStoreBySellerIdQuery(string SellerId) : IRequest<Result<StoreDto>>;
