using BuildingBlocks.Infrastructure.Models;
using MediatR;
using Media.API.Application.DTOs;

namespace Media.API.Application.Commands.UpdateGalleryOrder;

public sealed record UpdateGalleryOrderCommand(
    Guid TargetId,
    string TargetType,
    List<GalleryOrderItem> Items) : IRequest<Result<bool>>;
