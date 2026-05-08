namespace BuildingBlocks.Infrastructure.Models;

/// <summary>
/// Standard paginated response wrapper. Used by all query endpoints.
/// </summary>
/// <typeparam name="T">The type of the items in the paginated result.</typeparam>
/// <param name="Items">The items for the current page.</param>
/// <param name="TotalCount">The total number of items available across all pages.</param>
/// <param name="Page">The current page number.</param>
/// <param name="PageSize">The number of items per page.</param>
public sealed record PagedResult<T>(
	IReadOnlyList<T> Items,
	int TotalCount,
	int Page,
	int PageSize)
{
	/// <summary>
	/// Gets the total number of pages based on the <see cref="TotalCount"/> and <see cref="PageSize"/>.
	/// </summary>
	public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

	/// <summary>
	/// Indicates whether there is a previous page available.
	/// </summary>
	public bool HasPrevious => Page > 1;

	/// <summary>
	/// Indicates whether there is a next page available.
	/// </summary>
	public bool HasNext => Page < TotalPages;
}
