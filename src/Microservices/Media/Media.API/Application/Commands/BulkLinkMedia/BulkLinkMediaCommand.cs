using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Media.API.Application.Commands.BulkLinkMedia;

/// <summary>
/// Links an existing media item to multiple SKUs simultaneously.
/// This solves the variant matrix problem (e.g., applying the "Red" image
/// to the 128GB, 256GB, and 512GB SKUs without uploading it multiple times).
/// </summary>
public sealed record BulkLinkMediaCommand(
    Guid MediaItemId,
    List<Guid> SkuIds,
    bool IsPrimary) : IRequest<Result<bool>>;
