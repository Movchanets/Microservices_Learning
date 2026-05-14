using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;

namespace Catalog.Application.Interfaces;

public interface IProductReadRepository
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ProductListDto>> ListAsync(
        int page, int pageSize,
        Guid? categoryId = null,
        Guid? sellerId = null,
        string? search = null,
        CancellationToken ct = default);
}
