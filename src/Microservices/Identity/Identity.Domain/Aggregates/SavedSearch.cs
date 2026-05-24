using BuildingBlocks.SharedContracts.Abstractions;

namespace Identity.Domain.Aggregates;

public class SavedSearch : Entity
{
    public Guid UserId { get; private set; }
    public string Query { get; private set; } = string.Empty;
    public string FiltersJson { get; private set; } = "{}";
    public bool PriceAlertEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SavedSearch() { }

    public static SavedSearch Create(Guid userId, string query, string filtersJson, bool priceAlertEnabled = false)
    {
        return new SavedSearch
        {
            UserId = userId,
            Query = query,
            FiltersJson = filtersJson,
            PriceAlertEnabled = priceAlertEnabled,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void TogglePriceAlert(bool enabled)
    {
        PriceAlertEnabled = enabled;
    }
}
