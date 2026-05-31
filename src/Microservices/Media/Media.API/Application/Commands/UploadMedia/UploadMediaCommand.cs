using BuildingBlocks.Infrastructure.Models;
using Media.API.Application.DTOs;
using MediatR;

namespace Media.API.Application.Commands.UploadMedia;

public sealed record UploadMediaCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    Guid TargetId,
    string TargetType,
    bool IsPrimary,
    string? CreatedBy = null) : IRequest<Result<MediaItemDto>>;
