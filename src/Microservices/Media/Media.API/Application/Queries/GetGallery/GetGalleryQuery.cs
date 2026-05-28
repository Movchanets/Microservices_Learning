using BuildingBlocks.Infrastructure.Models;
using Media.API.Application.DTOs;
using MediatR;

namespace Media.API.Application.Queries.GetGallery;

public sealed record GetGalleryQuery(
    Guid TargetId,
    string TargetType) : IRequest<Result<List<MediaItemDto>>>;
