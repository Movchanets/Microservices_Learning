using System.Text.Json;
using System.Text.Json.Serialization;
using Cart.Domain.Aggregates;

namespace Cart.Infrastructure.Serialization;

public sealed class ShoppingCartJsonConverter : JsonConverter<ShoppingCart>
{
    public override ShoppingCart? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Support both PascalCase and camelCase
        var buyerId = GetStringProperty(root, "BuyerId", "buyerId");
        var id = GetNullableGuidProperty(root, "Id", "id");

        var cart = new ShoppingCart(buyerId);

        // Restore the original Id so CartItem.CartId stays consistent after cache round-trip
        if (id.HasValue)
        {
            var idProp = typeof(ShoppingCart).GetProperty("Id")!;
            idProp.SetValue(cart, id.Value);
        }

        if (TryGetProperty(root, "Items", "items", out var itemsElement))
        {
            foreach (var item in itemsElement.EnumerateArray())
            {
                var sku = GetStringProperty(item, "Sku", "sku");
                var quantity = GetInt32Property(item, "Quantity", "quantity");
                var price = GetDecimalProperty(item, "Price", "price");
                var shopId = GetNullableStringProperty(item, "ShopId", "shopId");
                cart.AddItem(sku, quantity, price, shopId);
            }
        }

        return cart;
    }

    public override void Write(Utf8JsonWriter writer, ShoppingCart value, JsonSerializerOptions options)
    {
        var namingPolicy = options.PropertyNamingPolicy;

        writer.WriteStartObject();

        writer.WriteString(ConvertName(namingPolicy, "Id"), value.Id);
        writer.WriteString(ConvertName(namingPolicy, "BuyerId"), value.BuyerId);
        writer.WriteNumber(ConvertName(namingPolicy, "Version"), value.Version);
        writer.WriteString(ConvertName(namingPolicy, "CreatedAt"), value.CreatedAt);
        writer.WriteString(ConvertName(namingPolicy, "UpdatedAt"), value.UpdatedAt);

        writer.WritePropertyName(ConvertName(namingPolicy, "Items"));
        writer.WriteStartArray();
        foreach (var item in value.Items)
        {
            writer.WriteStartObject();
            writer.WriteString(ConvertName(namingPolicy, "Sku"), item.Sku);
            writer.WriteNumber(ConvertName(namingPolicy, "Quantity"), item.Quantity);
            writer.WriteNumber(ConvertName(namingPolicy, "Price"), item.Price);
            if (item.ShopId is not null)
                writer.WriteString(ConvertName(namingPolicy, "ShopId"), item.ShopId);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private static string ConvertName(JsonNamingPolicy? policy, string name) =>
        policy?.ConvertName(name) ?? name;

    private static string GetStringProperty(JsonElement element, string name1, string name2)
    {
        if (element.TryGetProperty(name1, out var prop))
            return prop.GetString()!;
        if (element.TryGetProperty(name2, out prop))
            return prop.GetString()!;
        return string.Empty;
    }

    private static int GetInt32Property(JsonElement element, string name1, string name2)
    {
        if (element.TryGetProperty(name1, out var prop))
            return prop.GetInt32();
        if (element.TryGetProperty(name2, out prop))
            return prop.GetInt32();
        return 0;
    }

    private static decimal GetDecimalProperty(JsonElement element, string name1, string name2)
    {
        if (element.TryGetProperty(name1, out var prop))
            return prop.GetDecimal();
        if (element.TryGetProperty(name2, out prop))
            return prop.GetDecimal();
        return 0m;
    }

    private static string? GetNullableStringProperty(JsonElement element, string name1, string name2)
    {
        if (element.TryGetProperty(name1, out var prop))
            return prop.GetString();
        if (element.TryGetProperty(name2, out prop))
            return prop.GetString();
        return null;
    }

    private static bool TryGetProperty(JsonElement element, string name1, string name2, out JsonElement value)
    {
        if (element.TryGetProperty(name1, out value))
            return true;
        if (element.TryGetProperty(name2, out value))
            return true;
        value = default;
        return false;
    }

    private static Guid? GetNullableGuidProperty(JsonElement element, string name1, string name2)
    {
        if (element.TryGetProperty(name1, out var prop) && prop.TryGetGuid(out var g1))
            return g1;
        if (element.TryGetProperty(name2, out prop) && prop.TryGetGuid(out var g2))
            return g2;
        return null;
    }
}
