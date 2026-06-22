using Microsoft.Extensions.Logging;

namespace Seeder.App.Pipeline;

/// <summary>
/// Holds auth token and store ID for a seller.
/// Built during steps 1-2, immutable after that.
/// </summary>
public record SellerContext(string Token, Guid StoreId);

/// <summary>
/// Manages seller authentication tokens and store IDs.
/// Populated by UserStep and StoreStep, read by all subsequent steps.
/// </summary>
public class SellerRegistry
{
    private readonly Dictionary<string, SellerContext> _sellers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger _logger;

    public SellerRegistry(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a seller with their auth token (store ID set later by StoreStep).
    /// </summary>
    public void Register(string email, string token)
    {
        _sellers[email] = new SellerContext(token, Guid.Empty);
        _logger.LogInformation("Seller registered: {Email}", email);
    }

    /// <summary>
    /// Updates the store ID for a seller.
    /// </summary>
    public void SetStoreId(string email, Guid storeId)
    {
        if (_sellers.TryGetValue(email, out var ctx))
            _sellers[email] = ctx with { StoreId = storeId };
    }

    /// <summary>
    /// Gets the seller context by email.
    /// </summary>
    public SellerContext? GetByEmail(string email)
    {
        return _sellers.TryGetValue(email, out var ctx) ? ctx : null;
    }

    /// <summary>
    /// Resolves seller context by store name.
    /// Matches by email prefix: "store.tech@" → "Tech Store".
    /// </summary>
    public SellerContext? ResolveByStoreName(string storeName)
    {
        // Try email prefix match: "store.tech@" → "tech" matches "Tech Store"
        foreach (var (email, ctx) in _sellers)
        {
            var prefix = email.Split('@')[0].Replace("store.", "");
            if (storeName.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                return ctx;
        }

        // Fallback: first word of store name matches email prefix
        var firstWord = storeName.Split(' ')[0];
        foreach (var (email, ctx) in _sellers)
        {
            var storePrefix = email.Split('@')[0].Replace("store.", "");
            if (storePrefix.StartsWith(firstWord, StringComparison.OrdinalIgnoreCase)
                || firstWord.StartsWith(storePrefix, StringComparison.OrdinalIgnoreCase))
                return ctx;
        }

        return null;
    }
}
