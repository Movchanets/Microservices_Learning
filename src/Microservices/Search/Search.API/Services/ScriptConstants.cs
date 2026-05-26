namespace Search.API.Services;

/// <summary>
/// Painless scripts for Elasticsearch atomic updates.
/// Centralized here to avoid duplication and enable reuse.
/// </summary>
internal static class ScriptConstants
{
    /// <summary>
    /// Extends the price range (min/max) if the new price falls outside the current range.
    /// Does NOT shrink the range — shrinking requires knowing all SKU prices.
    /// </summary>
    public const string ExtendPriceRange =
        "if (params.price < ctx._source.minPrice || ctx._source.minPrice == 0) { ctx._source.minPrice = params.price; } " +
        "if (params.price > ctx._source.maxPrice) { ctx._source.maxPrice = params.price; } " +
        "ctx._source.currency = params.currency";

    /// <summary>
    /// Increments skuCount and extends the price range atomically.
    /// Used when a new SKU is added to a product.
    /// </summary>
    public const string AddSku =
        "ctx._source.skuCount += 1; " + ExtendPriceRange;

    /// <summary>
    /// Decrements skuCount (guarded at 0). Used when a SKU is removed.
    /// Price range is NOT recalculated — that requires re-querying Catalog.
    /// </summary>
    public const string RemoveSku =
        "if (ctx._source.skuCount > 0) { ctx._source.skuCount -= 1; }";

    public static Dictionary<string, object> PriceParams(decimal price, string currency) => new()
    {
        ["price"] = (double)price,
        ["currency"] = currency,
    };
}
