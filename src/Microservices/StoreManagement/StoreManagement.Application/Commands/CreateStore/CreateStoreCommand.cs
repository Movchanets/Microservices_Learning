using BuildingBlocks.Infrastructure.Models;
using MediatR;
using StoreManagement.Application.DTOs;

namespace StoreManagement.Application.Commands.CreateStore;

public sealed record CreateStoreCommand(
    string SellerId,
    string Name,
    string Description) : IRequest<Result<StoreDto>>;
