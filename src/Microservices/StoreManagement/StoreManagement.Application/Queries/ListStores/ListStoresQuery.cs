using BuildingBlocks.Infrastructure.Models;
using MediatR;
using StoreManagement.Application.DTOs;

namespace StoreManagement.Application.Queries.ListStores;

public sealed record ListStoresQuery(
    string? Status = null) : IRequest<Result<IReadOnlyList<StoreListDto>>>;
