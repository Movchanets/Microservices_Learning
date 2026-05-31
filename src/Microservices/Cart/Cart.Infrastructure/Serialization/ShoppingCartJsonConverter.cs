using System.Text.Json;
using System.Text.Json.Serialization;
using Cart.Domain.Aggregates;

namespace Cart.Infrastructure.Serialization;

public sealed class ShoppingCartJsonConverter : JsonConverter<ShoppingCart>
{
    public override ShoppingCart? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var buyerId = GetGuid(root, "BuyerId", "buyerId", nullable: true);
        var id = GetGuid(root, "Id", "id", nullable: true);

        var cart = new ShoppingCart(buyerId);
        if (id.HasValue)
            typeof(ShoppingCart).GetProperty("Id")!.SetValue(cart, id.Value);

        if (TryGet(root, "Items", "items", out var itemsElement))
        {
            foreach (var item in itemsElement.EnumerateArray())
            {
                cart.AddItem(
                    GetGuid(item, "ProductId", "productId")!.Value,
                    GetGuid(item, "SkuId", "skuId")!.Value,
                    GetString(item, "SkuCode", "skuCode") ?? "UNKNOWN",
                    GetInt(item, "Quantity", "quantity"),
                    GetGuid(item, "StoreId", "storeId")!.Value,
                    GetDecimal(item, "Price", "price"));
            }
        }

        return cart;
    }

    public override void Write(Utf8JsonWriter writer, ShoppingCart value, JsonSerializerOptions options)
    {
        var n = options.PropertyNamingPolicy;
        writer.WriteStartObject();
        writer.WriteString(N(n, "Id"), value.Id);
        if (value.BuyerId.HasValue) writer.WriteString(N(n, "BuyerId"), value.BuyerId.Value);
        else writer.WriteNull(N(n, "BuyerId"));
        writer.WriteNumber(N(n, "Version"), value.Version);
        writer.WriteString(N(n, "CreatedAt"), value.CreatedAt);
        writer.WriteString(N(n, "UpdatedAt"), value.UpdatedAt);

        writer.WritePropertyName(N(n, "Items"));
        writer.WriteStartArray();
        foreach (var item in value.Items)
        {
            writer.WriteStartObject();
            writer.WriteString(N(n, "ProductId"), item.ProductId);
            writer.WriteString(N(n, "SkuId"), item.SkuId);
            writer.WriteString(N(n, "SkuCode"), item.SkuCode);
            writer.WriteString(N(n, "StoreId"), item.StoreId);
            writer.WriteNumber(N(n, "Quantity"), item.Quantity);
            writer.WriteNumber(N(n, "Price"), item.Price);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    // ── Helpers ── Try PascalCase first, then camelCase ──

    private static string N(JsonNamingPolicy? policy, string name) => policy?.ConvertName(name) ?? name;

    private static bool TryGet(JsonElement el, string p1, string p2, out JsonElement v) =>
        el.TryGetProperty(p1, out v) || el.TryGetProperty(p2, out v);

    private static string? GetString(JsonElement el, string p1, string p2) =>
        TryGet(el, p1, p2, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int GetInt(JsonElement el, string p1, string p2) =>
        TryGet(el, p1, p2, out var v) ? v.GetInt32() : 0;

    private static decimal GetDecimal(JsonElement el, string p1, string p2) =>
        TryGet(el, p1, p2, out var v) ? v.GetDecimal() : 0m;

    private static Guid? GetGuid(JsonElement el, string p1, string p2, bool nullable = false)
    {
        if (!TryGet(el, p1, p2, out var v)) return nullable ? null : Guid.Empty;
        if (v.ValueKind == JsonValueKind.Null) return null;
        return v.TryGetGuid(out var g) ? g : (nullable ? null : Guid.Empty);
    }
}
