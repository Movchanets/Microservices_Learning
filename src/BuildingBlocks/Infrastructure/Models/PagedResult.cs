namespace BuildingBlocks.Infrastructure.Models;

/// <summary>
/// Standard paginated response wrapper. Used by all query endpoints.
/// </summary>
public sealed record PagedResult<T>(
	IReadOnlyList<T> Items,
	int TotalCount,
	int Page,
	int PageSize)
{
	public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
	public bool HasPrevious => Page > 1;
	public bool HasNext => Page < TotalPages;
}
