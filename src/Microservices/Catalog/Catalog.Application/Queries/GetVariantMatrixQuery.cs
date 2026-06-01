using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Queries;

/// <summary>
/// Returns the variant matrix for a product — all possible SKU combinations
/// based on variant-axis attribute definitions, with availability info.
/// Used by the frontend to render the variant picker (color × storage grid).
/// </summary>
public sealed record GetVariantMatrixQuery(Guid ProductId)
    : IRequest<VariantMatrixDto?>;
